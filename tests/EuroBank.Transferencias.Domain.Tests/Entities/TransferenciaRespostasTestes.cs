using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaRespostasTestes
{
    [Fact]
    public void DebitoRealizado_PassaPorDebitadaEPedeALiquidacaoAoBancoExterno()
    {
        var transferencia = AguardandoDebito();

        transferencia.RegistrarDebitoRealizado(Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);
        transferencia.Historico.Select(passo => passo.EstadoApos).ShouldContain(EstadoTransferencia.Debitada);
        transferencia.TentativasNoPasso.ShouldBe(1);
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoLiquidacao);
        var liquidacao = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<LiquidacaoSolicitada>();
        liquidacao.IbanDestino.ShouldBe(IbanComum);
        liquidacao.Bic.ShouldBe("DEUTDEFF");
        liquidacao.NomeBeneficiario.ShouldBe("Bruno");
        liquidacao.Valor.ShouldBe(Dinheiro.De(100m));
        liquidacao.Tentativa.ShouldBe(1);
    }

    [Fact]
    public void DebitoRealizado_Repetido_EIgnoradoEFicaNoHistorico()
    {
        var transferencia = AguardandoLiquidacao();
        var passos = transferencia.Historico.Count;

        transferencia.RegistrarDebitoRealizado(Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);
        transferencia.EventosDominio.ShouldBeEmpty();
        transferencia.Historico.Count.ShouldBe(passos + 1);
    }

    [Theory]
    [InlineData(MotivoTransferencia.SaldoInsuficiente)]
    [InlineData(MotivoTransferencia.ContaBloqueada)]
    [InlineData(MotivoTransferencia.ContaEncerrada)]
    [InlineData(MotivoTransferencia.ContaInexistente)]
    public void DebitoRecusado_TerminaRecusadaComOMotivoDeContas(MotivoTransferencia motivo)
    {
        var transferencia = AguardandoDebito();

        transferencia.RegistrarDebitoRecusado(motivo, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(motivo);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecusada>();
    }

    [Fact]
    public void LiquidacaoConfirmada_ConcluiELiberaATravaDaConta()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Concluida);
        transferencia.Motivo.ShouldBeNull();
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.OfType<LiberacaoTravaSolicitada>().ShouldHaveSingleItem().ContaOrigemId.ShouldBe(ContaOrigem);
        transferencia.EventosDominio.OfType<TransferenciaConcluida>().ShouldHaveSingleItem();
    }

    [Fact]
    public void LiquidacaoConfirmada_Duplicada_EIgnorada()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Concluida);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void LiquidacaoRecusada_PedeOEstornoDoDebito()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.RegistrarLiquidacaoRecusada(Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloBanco);
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoEstorno);
        var estorno = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<EstornoSolicitado>();
        estorno.ContaOrigemId.ShouldBe(ContaOrigem);
        estorno.Tentativa.ShouldBe(1);
    }

    [Fact]
    public void EstornoRealizado_TerminaCanceladaComOMotivoDaFalha()
    {
        var transferencia = AguardandoEstorno();

        transferencia.RegistrarEstornoRealizado(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloBanco);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCancelada>()
            .Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloBanco);
    }

    [Theory]
    [InlineData(StatusLiquidacaoExterna.Liquidada, EstadoTransferencia.Concluida, null)]
    [InlineData(StatusLiquidacaoExterna.NaoLiquidada, EstadoTransferencia.AguardandoEstorno, MotivoTransferencia.SemRespostaDoBanco)]
    [InlineData(StatusLiquidacaoExterna.Recusada, EstadoTransferencia.AguardandoEstorno, MotivoTransferencia.RecusadaPeloBanco)]
    public void StatusDaConsulta_DecideEntreConcluirEEstornar(
        StatusLiquidacaoExterna status,
        EstadoTransferencia estadoEsperado,
        MotivoTransferencia? motivoEsperado)
    {
        var transferencia = ConsultandoLiquidacao();

        transferencia.RegistrarStatusLiquidacao(status, Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(estadoEsperado);
        transferencia.Motivo.ShouldBe(motivoEsperado);
    }

    [Fact]
    public void StatusDaConsulta_ForaDaConsulta_EIgnorado()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.RegistrarStatusLiquidacao(StatusLiquidacaoExterna.NaoLiquidada, Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);
    }

    [Fact]
    public void LiquidacaoConfirmadaDuranteAConsulta_Conclui()
    {
        var transferencia = ConsultandoLiquidacao();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Concluida);
    }

    [Fact]
    public void CaminhoFeliz_HistoricoMostraCadaEstadoEAVersaoCresceACadaPasso()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);

        transferencia.Historico.Select(passo => passo.EstadoApos).Distinct().ShouldBe(
        [
            EstadoTransferencia.Agendada,
            EstadoTransferencia.AguardandoDebito,
            EstadoTransferencia.Debitada,
            EstadoTransferencia.AguardandoLiquidacao,
            EstadoTransferencia.Concluida,
        ]);
        transferencia.Versao.ShouldBe(transferencia.Historico.Count);
        transferencia.Historico.Select(passo => passo.Ordem).ShouldBe(Enumerable.Range(1, transferencia.Historico.Count));
    }
}
