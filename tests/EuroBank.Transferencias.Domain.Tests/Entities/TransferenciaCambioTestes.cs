using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.ValueObjects;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaCambioTestes
{
    [Fact]
    public void ExternaEmOutraMoeda_ComecaCotando_SemValorDebitadoAinda()
    {
        var transferencia = NovaExterna(valor: 100m, moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);

        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cotando);
        transferencia.ValorDebitado.ShouldBeNull();
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoCotacao);
        var pedido = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<CotacaoSolicitada>();
        pedido.MoedaOrigem.ShouldBe(Moeda.EUR);
        pedido.MoedaDestino.ShouldBe(Moeda.USD);
        pedido.Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
    }

    [Fact]
    public void CotacaoEmitida_TravaATaxaEPedeODebitoNaMoedaDaContaComAConversao()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        transferencia.LimparEventos();

        CotarCem(transferencia);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.Cotacao.ShouldNotBeNull().Taxa.ShouldBe(1.0692m);
        transferencia.ValorDebitado.ShouldBe(Dinheiro.De(93.53m));
        transferencia.ValorDestino.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        var debito = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<DebitoSolicitado>();
        debito.Valor.ShouldBe(Dinheiro.De(93.53m));
        debito.Tarifa.ShouldBe(Dinheiro.De(2.50m));
        debito.ValorDestino.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        debito.Taxa.ShouldBe(1.0692m);
    }

    [Fact]
    public void LiquidacaoComCambio_VaiNaMoedaDeDestino()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        CotarCem(transferencia);
        transferencia.LimparEventos();

        transferencia.RegistrarDebitoRealizado(Politica, Agora);

        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<LiquidacaoSolicitada>()
            .Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
    }

    [Fact]
    public void CotacaoQueChegaVencida_CotaDeNovo()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarCotacaoEmitida(
            new CotacaoTravada(Guid.CreateVersion7(), 1.0692m, Agora.AddSeconds(-1)),
            Dinheiro.De(93.53m), Dinheiro.De(100m, Moeda.USD), Dinheiro.Zero, Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cotando);
        transferencia.Cotacao.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<CotacaoSolicitada>();
    }

    [Fact]
    public void CotacaoIndisponivel_RecusaSemDebito()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarCotacaoIndisponivel("par indisponível", Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CambioIndisponivel);
        transferencia.EventosDominio.OfType<DebitoSolicitado>().ShouldBeEmpty();
    }

    [Fact]
    public void CambioSemResposta_RecusaDepoisDosReenvios()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);

        EsgotarPrazos(transferencia, Politica.PrazoCotacao);

        transferencia.EventosDominio.OfType<CotacaoSolicitada>().Count().ShouldBe(4);
        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CambioIndisponivel);
    }

    [Fact]
    public void LimiteComCambio_ConfereOValorQueSaiDaConta()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);

        CotarCem(transferencia, jaTransferido: 4_950m);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.LimiteDiarioExcedido);
    }

    [Fact]
    public void CancelarCotando_TerminaCancelada_EACotacaoTardiaEIgnorada()
    {
        var transferencia = NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.USD);
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);

        transferencia.SolicitarCancelamento(Agora);
        CotarCem(transferencia);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
        transferencia.EventosDominio.OfType<DebitoSolicitado>().ShouldBeEmpty();
    }

    [Fact]
    public void TarifaELimite_SaoDaMoedaDaContaDeOrigem()
    {
        var emDolar = new ContaEuroBank(Guid.CreateVersion7(), "PT50EURO0000000000009", "Caio", Moeda.USD);

        var transferencia = NovaExterna(valor: 5_400m, origem: emDolar);
        transferencia.Iniciar(Dinheiro.ZeroEm(Moeda.USD), Politica, Agora);

        transferencia.Tarifa.ShouldBe(Dinheiro.De(2.70m, Moeda.USD));
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
    }

    [Fact]
    public void ValorNumaTerceiraMoeda_Recusa()
    {
        Should.Throw<ExcecaoMoedaDoValorInvalida>(() => NovaExterna(moedaDestino: Moeda.USD, moedaDoValor: Moeda.GBP));
    }

    [Fact]
    public void CambioInterno_CotaDebitaCreditaEConcluiLiberandoATrava()
    {
        var transferencia = AguardandoCredito();

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoCredito);
        transferencia.Tipo.ShouldBe(TipoTransferencia.CambioInterno);

        transferencia.RegistrarCreditoRealizado(Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Concluida);
        transferencia.EventosDominio.OfType<LiberacaoTravaSolicitada>().ShouldHaveSingleItem().ContaOrigemId.ShouldBe(ContaOrigem);
        transferencia.EventosDominio.OfType<TransferenciaConcluida>().ShouldHaveSingleItem();
    }

    [Fact]
    public void CambioInterno_PedeOCreditoNaContaDeDestinoNaMoedaDela()
    {
        var transferencia = NovoCambioInterno();
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        CotarCem(transferencia);
        transferencia.LimparEventos();

        transferencia.RegistrarDebitoRealizado(Politica, Agora);

        var credito = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<CreditoSolicitado>();
        credito.ContaDestinoId.ShouldBe(DestinoEmDolar.Id);
        credito.Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        credito.ValorOrigem.ShouldBe(Dinheiro.De(93.53m));
        credito.NomeOrigem.ShouldBe("Ana");
        credito.Taxa.ShouldBe(1.0692m);
    }

    [Fact]
    public void CambioInterno_DestinoEncerrado_EstornaAOrigem()
    {
        var transferencia = AguardandoCredito();

        transferencia.RegistrarCreditoRecusado(MotivoTransferencia.ContaEncerrada, Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.DestinoEncerrado);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<EstornoSolicitado>();

        transferencia.RegistrarEstornoRealizado(Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.DestinoEncerrado);
    }

    [Fact]
    public void CambioInterno_PrazoDoCredito_ReenviaSempre()
    {
        var transferencia = AguardandoCredito();
        var instante = Agora;

        for (var vez = 0; vez < 8; vez++)
        {
            instante += Politica.PrazoCredito;
            transferencia.VerificarPrazo(Politica, instante);
        }

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoCredito);
        transferencia.EventosDominio.OfType<CreditoSolicitado>().Count().ShouldBe(8);
    }

    [Fact]
    public void CambioInterno_DepoisDePedirOCredito_NaoPodeCancelar()
    {
        Should.Throw<ExcecaoCancelamentoNaoPermitido>(() => AguardandoCredito().SolicitarCancelamento(Agora));
    }

    [Fact]
    public void CambioInterno_NaMesmaMoeda_Recusa()
    {
        var mesmaMoeda = DestinoEmDolar with { Moeda = Moeda.EUR };

        Should.Throw<ExcecaoCambioNaMesmaMoeda>(() => Domain.Entities.Transferencia.SolicitarCambioInterno(
            Origem, mesmaMoeda, Dinheiro.De(10m), null, null, "k", "h", null, Agora));
    }

    [Fact]
    public void CambioInterno_ParaAPropriaConta_Recusa()
    {
        var mesmaConta = Origem with { Moeda = Moeda.USD };

        Should.Throw<ExcecaoDestinoIgualOrigem>(() => Domain.Entities.Transferencia.SolicitarCambioInterno(
            Origem, mesmaConta, Dinheiro.De(10m), null, null, "k", "h", null, Agora));
    }

    [Fact]
    public void CambioInterno_ComIbanDeFora_Recusa()
    {
        var deFora = DestinoEmDolar with { Iban = "DE89370400440532013000" };

        Should.Throw<ExcecaoDestinoNaoEuroBank>(() => Domain.Entities.Transferencia.SolicitarCambioInterno(
            Origem, deFora, Dinheiro.De(10m, Moeda.USD), null, null, "k", "h", null, Agora));
    }
}
