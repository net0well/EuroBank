using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.ValueObjects;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>Os eventos finais levam o retrato da transferência, para quem notifica não precisar consultar a saga.</summary>
public sealed class TransferenciaResumoTestes
{
    private static readonly ResumoTransferencia ResumoDaExterna = new(
        TipoTransferencia.Externa,
        "Ana",
        "Bruno",
        IbanComum,
        Dinheiro.De(100m),
        Dinheiro.De(2.50m),
        Dinheiro.De(100m),
        Dinheiro.De(100m),
        Moeda.EUR,
        null,
        "Aluguel",
        EmailCliente);

    [Fact]
    public void Concluida_LevaOResumo_SemMotivo()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.RegistrarLiquidacaoConfirmada(Agora);

        var concluida = transferencia.EventosDominio.OfType<TransferenciaConcluida>().ShouldHaveSingleItem();
        concluida.Resumo.ShouldBe(ResumoDaExterna);
        concluida.Motivo.ShouldBeNull();
    }

    [Fact]
    public void ConcluidaPorReconciliacao_LevaOMotivoReconciliada()
    {
        var transferencia = Divergente();
        transferencia.SolicitarReconciliacao(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarReconciliacaoRealizada(Agora);

        var concluida = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaConcluida>();
        concluida.Motivo.ShouldBe(MotivoTransferencia.Reconciliada);
        concluida.Resumo.ShouldBe(ResumoDaExterna);
    }

    [Fact]
    public void Recusada_LevaOResumo()
    {
        var transferencia = AguardandoDebito();

        transferencia.RegistrarDebitoRecusado(MotivoTransferencia.SaldoInsuficiente, Agora);

        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecusada>()
            .Resumo.ShouldBe(ResumoDaExterna);
    }

    [Fact]
    public void Cancelada_LevaOResumo()
    {
        var transferencia = AguardandoEstorno();

        transferencia.RegistrarEstornoRealizado(Agora);

        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCancelada>()
            .Resumo.ShouldBe(ResumoDaExterna);
    }

    [Fact]
    public void Divergente_LevaOResumo()
    {
        var transferencia = CanceladaSemRespostaDoBanco();

        transferencia.RegistrarLiquidacaoConfirmada(Agora);

        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDivergente>()
            .Resumo.ShouldBe(ResumoDaExterna);
    }

    [Fact]
    public void Devolvida_LevaOResumo()
    {
        var transferencia = Concluida();
        transferencia.RegistrarDevolucaoRecebida(Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarDevolucaoCreditada(Agora);

        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaDevolvida>()
            .Resumo.ShouldBe(ResumoDaExterna);
    }

    [Fact]
    public void CambioInterno_ResumoTemOsDoisLadosEATaxa()
    {
        var transferencia = AguardandoCredito();

        transferencia.RegistrarCreditoRealizado(Agora);

        var resumo = transferencia.EventosDominio.OfType<TransferenciaConcluida>().ShouldHaveSingleItem().Resumo;
        resumo.Tipo.ShouldBe(TipoTransferencia.CambioInterno);
        resumo.NomeBeneficiario.ShouldBe("Bruno");
        resumo.IbanDestino.ShouldBe(DestinoEmDolar.Iban);
        resumo.Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        resumo.ValorDebitado.ShouldBe(Dinheiro.De(93.53m, Moeda.EUR));
        resumo.ValorDestino.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        resumo.MoedaDestino.ShouldBe(Moeda.USD);
        resumo.Taxa.ShouldBe(1.0692m);
        resumo.EmailNotificacao.ShouldBe(EmailCliente);
    }

    [Fact]
    public void RecusadaAntesDeCotar_ResumoSemValoresConvertidos()
    {
        var transferencia = NovoCambioInterno();
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarCotacaoIndisponivel("par indisponível", Agora);

        var resumo = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecusada>().Resumo;
        resumo.Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        resumo.ValorDebitado.ShouldBeNull();
        resumo.ValorDestino.ShouldBeNull();
        resumo.MoedaDestino.ShouldBe(Moeda.USD);
        resumo.Taxa.ShouldBeNull();
    }
}
