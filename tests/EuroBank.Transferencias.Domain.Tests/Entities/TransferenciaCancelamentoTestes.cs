using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaCancelamentoTestes
{
    [Fact]
    public void Agendada_CancelaNaHora()
    {
        var transferencia = NovaExterna(executarEm: Agora.AddDays(1));

        transferencia.SolicitarCancelamento(Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCancelada>();
    }

    [Fact]
    public void CancelarDuasVezes_NaoFalha()
    {
        var transferencia = NovaExterna(executarEm: Agora.AddDays(1));
        transferencia.SolicitarCancelamento(Agora);
        transferencia.LimparEventos();

        transferencia.SolicitarCancelamento(Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void AguardandoDebito_AnotaEEstornaQuandoODebitoChega()
    {
        var transferencia = AguardandoDebito();

        transferencia.SolicitarCancelamento(Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.CancelamentoSolicitado.ShouldBeTrue();

        transferencia.RegistrarDebitoRealizado(Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
        transferencia.EventosDominio.OfType<LiquidacaoSolicitada>().ShouldBeEmpty();
        transferencia.EventosDominio.OfType<EstornoSolicitado>().ShouldHaveSingleItem();

        transferencia.RegistrarEstornoRealizado(Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
    }

    [Fact]
    public void AguardandoDebito_ComDebitoRecusado_TerminaCanceladaPeloCliente()
    {
        var transferencia = AguardandoDebito();
        transferencia.SolicitarCancelamento(Agora);

        transferencia.RegistrarDebitoRecusado(MotivoTransferencia.SaldoInsuficiente, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
    }

    [Fact]
    public void AguardandoDebito_SemResposta_TerminaCanceladaPeloClienteEAindaCompensaODebitoTardio()
    {
        var transferencia = AguardandoDebito();
        transferencia.SolicitarCancelamento(Agora);

        EsgotarPrazos(transferencia, Politica.PrazoDebito);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
        transferencia.DebitoSemResposta.ShouldBeTrue();

        transferencia.RegistrarDebitoRealizado(Politica, Agora.AddMinutes(10));

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
    }

    [Fact]
    public void DepoisDeAPedirALiquidacao_NaoPermite()
    {
        var transferencia = AguardandoLiquidacao();

        Should.Throw<ExcecaoCancelamentoNaoPermitido>(() => transferencia.SolicitarCancelamento(Agora));
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoLiquidacao);
    }

    [Fact]
    public void Concluida_NaoPermite()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);

        Should.Throw<ExcecaoCancelamentoNaoPermitido>(() => transferencia.SolicitarCancelamento(Agora));
    }
}
