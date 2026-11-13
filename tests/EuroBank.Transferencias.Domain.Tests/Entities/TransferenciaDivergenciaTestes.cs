using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>Confirmação do banco depois do estorno (Divergente) e a reconciliação que resolve.</summary>
public sealed class TransferenciaDivergenciaTestes
{
    [Fact]
    public void LiquidacaoConfirmada_DepoisDoEstorno_FicaDivergente()
    {
        var transferencia = CanceladaSemRespostaDoBanco();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Divergente);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDivergente>()
            .Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
    }

    [Fact]
    public void StatusLiquidada_DepoisDoEstorno_TambemFicaDivergente()
    {
        var transferencia = CanceladaSemRespostaDoBanco();

        transferencia.RegistrarStatusLiquidacao(StatusLiquidacaoExterna.Liquidada, Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Divergente);
    }

    [Fact]
    public void LiquidacaoConfirmada_ComOEstornoEmAndamento_SoAnotaEDivergeQuandoOEstornoTermina()
    {
        var transferencia = AguardandoEstorno();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Aplicado);
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.LiquidacaoConfirmadaNoEstorno.ShouldBeTrue();
        transferencia.EventosDominio.ShouldBeEmpty();

        transferencia.RegistrarEstornoRealizado(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Divergente);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDivergente>();
    }

    [Fact]
    public void LiquidacaoConfirmada_DepoisDeUmCancelamentoDoCliente_EIgnorada()
    {
        var transferencia = AguardandoDebito();
        transferencia.SolicitarCancelamento(Agora);
        transferencia.RegistrarDebitoRealizado(Politica, Agora);
        transferencia.RegistrarEstornoRealizado(Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
    }

    [Fact]
    public void LiquidacaoConfirmada_RepetidaNaDivergente_EIgnorada()
    {
        var transferencia = Divergente();

        transferencia.RegistrarLiquidacaoConfirmada(Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Reconciliacao_PedeAContasODebitoDeNovo()
    {
        var transferencia = Divergente();

        transferencia.SolicitarReconciliacao(Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Reconciliando);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoReconciliacao);
        var comando = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ReconciliacaoSolicitada>();
        comando.ContaOrigemId.ShouldBe(ContaOrigem);
        comando.Tentativa.ShouldBe(1);
    }

    [Fact]
    public void ReconciliacaoRealizada_ConcluiComoReconciliada_SemLiberarATrava()
    {
        var transferencia = Divergente();
        transferencia.SolicitarReconciliacao(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarReconciliacaoRealizada(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Concluida);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.Reconciliada);
        transferencia.PrazoDoPasso.ShouldBeNull();
        var concluida = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaConcluida>();
        concluida.Valor.ShouldBe(Dinheiro.De(100m));
        concluida.Tarifa.ShouldBe(Dinheiro.De(2.50m));
    }

    [Fact]
    public void ReconciliacaoRecusada_VoltaParaDivergente_EPodeSerPedidaDeNovo()
    {
        var transferencia = Divergente();
        transferencia.SolicitarReconciliacao(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarReconciliacaoRecusada(MotivoTransferencia.ContaEncerrada, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Divergente);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.ConfirmadaAposEstorno);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldBeEmpty();
        transferencia.Historico[^1].Descricao.ShouldContain("ContaEncerrada");
        Should.NotThrow(() => transferencia.SolicitarReconciliacao(Politica, Agora));
    }

    [Fact]
    public void Reconciliacao_Repetida_NaoMudaNada()
    {
        var transferencia = Divergente();
        transferencia.SolicitarReconciliacao(Politica, Agora);
        var versaoReconciliando = transferencia.Versao;

        transferencia.SolicitarReconciliacao(Politica, Agora);
        transferencia.Versao.ShouldBe(versaoReconciliando);

        transferencia.RegistrarReconciliacaoRealizada(Agora);
        transferencia.LimparEventos();
        var versaoConcluida = transferencia.Versao;

        transferencia.SolicitarReconciliacao(Politica, Agora);

        transferencia.Versao.ShouldBe(versaoConcluida);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Reconciliacao_DeUmaConcluidaNormal_ENaoPermitida()
    {
        var transferencia = Concluida();

        Should.Throw<ExcecaoReconciliacaoNaoPermitida>(() => transferencia.SolicitarReconciliacao(Politica, Agora));
    }

    [Fact]
    public void Reconciliando_SemResposta_ReenviaSemLimite()
    {
        var transferencia = Divergente();
        transferencia.SolicitarReconciliacao(Politica, Agora);
        transferencia.LimparEventos();

        var instante = Agora;
        for (var vez = 0; vez < Politica.ReenviosPorPasso + 3; vez++)
        {
            instante += Politica.PrazoReconciliacao;
            transferencia.VerificarPrazo(Politica, instante).ShouldBe(ResultadoPasso.Aplicado);
        }

        transferencia.Estado.ShouldBe(EstadoTransferencia.Reconciliando);
        transferencia.EventosDominio.OfType<ReconciliacaoSolicitada>().Count().ShouldBe(Politica.ReenviosPorPasso + 3);
    }
}
