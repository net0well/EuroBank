using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>O banco externo devolve uma transferência já concluída (o caminho do final 94).</summary>
public sealed class TransferenciaDevolucaoTestes
{
    [Fact]
    public void DevolucaoRecebida_PedeAContasOCreditoNaOrigem()
    {
        var transferencia = Concluida();

        transferencia.RegistrarDevolucaoRecebida(Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoCreditoDevolucao);
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoCreditoDevolucao);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<CreditoDevolucaoSolicitado>()
            .ContaOrigemId.ShouldBe(ContaOrigem);
    }

    [Fact]
    public void DevolucaoCreditada_TerminaDevolvidaComOValorSemATarifa()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarDevolucaoCreditada(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Devolvida);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.DevolvidaPeloBanco);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDevolvida>()
            .ValorDevolvido.ShouldBe(Dinheiro.De(100m));
    }

    [Fact]
    public void DevolucaoRecusada_FicaDivergente_ENaoPodeSerReconciliada()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarDevolucaoRecusada(MotivoTransferencia.ContaEncerrada, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Divergente);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.DevolucaoNaoCreditada);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDivergente>();
        Should.Throw<ExcecaoReconciliacaoNaoPermitida>(() => transferencia.SolicitarReconciliacao(Politica, Agora));
    }

    [Fact]
    public void DevolucaoRecebida_AntesDaConclusao_EIgnorada()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.RegistrarDevolucaoRecebida(Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void DevolucaoRecebida_Repetida_EIgnorada()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarDevolucaoRecebida(Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void CreditoDaDevolucao_SemResposta_ReenviaSemLimite()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);
        transferencia.LimparEventos();

        var instante = EsgotarPrazos(transferencia, Politica.PrazoCreditoDevolucao);
        transferencia.VerificarPrazo(Politica, instante + Politica.PrazoCreditoDevolucao).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoCreditoDevolucao);
        transferencia.EventosDominio.OfType<CreditoDevolucaoSolicitado>().Count().ShouldBe(Politica.ReenviosPorPasso + 2);
    }

    [Fact]
    public void ComADevolucaoEmAndamento_NaoPodeCancelar()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);

        Should.Throw<ExcecaoCancelamentoNaoPermitido>(() => transferencia.SolicitarCancelamento(Agora));
    }
}
