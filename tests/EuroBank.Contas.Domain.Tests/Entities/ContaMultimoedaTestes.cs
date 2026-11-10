using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.Entities;

public sealed class ContaMultimoedaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static Conta NovaConta(Moeda moeda, decimal saldo = 0m, long sequencial = 1)
    {
        var conta = Conta.Abrir(Iban.Gerar(sequencial), Titular.Criar("Ana"), Agora, moeda);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo, moeda), Agora);
        }

        conta.LimparEventos();
        return conta;
    }

    private static Conversao EmDolares(decimal valor) => Conversao.Criar(Dinheiro.De(valor, Moeda.USD), 1.0692m);

    [Fact]
    public void Abrir_NaMoedaEscolhida_SaldoZeroNessaMoedaEEventoComAMoeda()
    {
        var conta = Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora, Moeda.USD);

        conta.Moeda.ShouldBe(Moeda.USD);
        conta.Saldo.ShouldBe(Dinheiro.ZeroEm(Moeda.USD));
        conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ContaAberta>().Moeda.ShouldBe(Moeda.USD);
    }

    [Fact]
    public void Abrir_SemMoeda_AbreEmEuro()
    {
        Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora).Moeda.ShouldBe(Moeda.EUR);
    }

    [Fact]
    public void DepositoOuSaqueEmOutraMoeda_LancaExcecaoMoedaDiferente()
    {
        var conta = NovaConta(Moeda.GBP, 100m);

        Should.Throw<ExcecaoMoedaDiferente>(() => conta.Depositar(Dinheiro.De(10m, Moeda.EUR), Agora));
        Should.Throw<ExcecaoMoedaDiferente>(() => conta.Sacar(Dinheiro.De(10m, Moeda.EUR), Agora));
        conta.Saldo.ShouldBe(Dinheiro.De(100m, Moeda.GBP));
    }

    [Fact]
    public void TransferenciaInternaEntreMoedas_Recusa_IndicandoOCambio()
    {
        var euros = NovaConta(Moeda.EUR, 100m, sequencial: 1);
        var dolares = NovaConta(Moeda.USD, sequencial: 2);

        var erro = Should.Throw<ExcecaoTransferenciaEntreMoedas>(
            () => new ServicoTransferencia().Transferir(euros, dolares, Dinheiro.De(10m), Agora));

        erro.Message.ShouldContain("/transferencias-cambio");
        euros.Saldo.ShouldBe(Dinheiro.De(100m));
        euros.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void DebitoExternoComConversao_RegistraOValorQueODestinoRecebe()
    {
        var conta = NovaConta(Moeda.EUR, 1_000m);

        conta.DebitarTransferenciaExterna(
            Guid.CreateVersion7(), Dinheiro.De(93.53m), Dinheiro.De(2.50m), "US64SVBKUS6S3300958879", "Bruno", null, Agora, EmDolares(100m));

        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaExternaEnviada>();
        evento.Conversao.ShouldNotBeNull().Contraparte.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        evento.Conversao.Taxa.ShouldBe(1.0692m);
        conta.Saldo.ShouldBe(Dinheiro.De(903.97m));
    }

    [Fact]
    public void DebitoDeCambio_DebitaValorETarifaTravaERegistraOEnvio()
    {
        var conta = NovaConta(Moeda.EUR, 1_000m);
        var transferenciaId = Guid.CreateVersion7();

        var resultado = conta.DebitarTransferenciaCambio(
            transferenciaId, Dinheiro.De(93.53m), Dinheiro.De(2.50m), "PT50EURO0000000000002", "Bruno", null, EmDolares(100m), Agora);

        resultado.ShouldBe(ResultadoOperacao.Aplicada);
        conta.Saldo.ShouldBe(Dinheiro.De(903.97m));
        conta.TemTransferenciaEmAndamento.ShouldBeTrue();
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCambioEnviada>();
        evento.Conversao.Contraparte.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        evento.IbanDestino.ShouldBe("PT50EURO0000000000002");
    }

    [Fact]
    public void DebitoDeCambio_Repetido_NaoDebitaDeNovo()
    {
        var conta = NovaConta(Moeda.EUR, 1_000m);
        var transferenciaId = Guid.CreateVersion7();
        conta.DebitarTransferenciaCambio(
            transferenciaId, Dinheiro.De(93.53m), Dinheiro.De(2.50m), "PT50EURO0000000000002", "Bruno", null, EmDolares(100m), Agora);
        conta.LimparEventos();

        conta.DebitarTransferenciaCambio(
            transferenciaId, Dinheiro.De(93.53m), Dinheiro.De(2.50m), "PT50EURO0000000000002", "Bruno", null, EmDolares(100m), Agora)
            .ShouldBe(ResultadoOperacao.JaProcessada);

        conta.EventosDominio.ShouldBeEmpty();
        conta.Saldo.ShouldBe(Dinheiro.De(903.97m));
    }

    [Fact]
    public void CreditoDeCambio_CreditaSemTravarERegistraORecebimento()
    {
        var conta = NovaConta(Moeda.USD);
        var origem = Conversao.Criar(Dinheiro.De(93.53m, Moeda.EUR), 1.0692m);

        var resultado = conta.CreditarTransferenciaCambio(
            Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null, origem, Agora);

        resultado.ShouldBe(ResultadoOperacao.Aplicada);
        conta.Saldo.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        conta.TemTransferenciaEmAndamento.ShouldBeFalse();
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCambioRecebida>();
        evento.Conversao.Contraparte.ShouldBe(Dinheiro.De(93.53m, Moeda.EUR));
        evento.IbanOrigem.ShouldBe("PT50EURO0000000000001");
    }

    [Fact]
    public void CreditoDeCambio_Repetido_CreditaUmaVezSo()
    {
        var conta = NovaConta(Moeda.USD);
        var transferenciaId = Guid.CreateVersion7();
        var origem = Conversao.Criar(Dinheiro.De(93.53m, Moeda.EUR), 1.0692m);
        conta.CreditarTransferenciaCambio(transferenciaId, Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null, origem, Agora);

        conta.CreditarTransferenciaCambio(transferenciaId, Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null, origem, Agora)
            .ShouldBe(ResultadoOperacao.JaProcessada);

        conta.Saldo.ShouldBe(Dinheiro.De(100m, Moeda.USD));
    }

    [Fact]
    public void CreditoDeCambio_EmContaBloqueada_Aceita_EmContaEncerrada_Recusa()
    {
        var origem = Conversao.Criar(Dinheiro.De(93.53m, Moeda.EUR), 1.0692m);
        var bloqueada = NovaConta(Moeda.USD);
        bloqueada.Bloquear(Agora);
        var encerrada = NovaConta(Moeda.USD, sequencial: 2);
        encerrada.Encerrar(Agora);

        bloqueada.CreditarTransferenciaCambio(Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null, origem, Agora)
            .ShouldBe(ResultadoOperacao.Aplicada);
        Should.Throw<ExcecaoContaEncerrada>(() => encerrada.CreditarTransferenciaCambio(
            Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null, origem, Agora));
    }

    [Fact]
    public void CreditoDeCambio_NaMoedaErrada_LancaExcecaoMoedaDiferente()
    {
        var conta = NovaConta(Moeda.USD);
        var origem = Conversao.Criar(Dinheiro.De(93.53m, Moeda.EUR), 1.0692m);

        Should.Throw<ExcecaoMoedaDiferente>(() => conta.CreditarTransferenciaCambio(
            Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.EUR), "PT50EURO0000000000001", "Ana", null, origem, Agora));
    }

    [Fact]
    public void CreditoRecebido_NaoPodeSerEstornado()
    {
        var conta = NovaConta(Moeda.USD);
        var transferenciaId = Guid.CreateVersion7();
        conta.CreditarTransferenciaCambio(transferenciaId, Dinheiro.De(100m, Moeda.USD), "PT50EURO0000000000001", "Ana", null,
            Conversao.Criar(Dinheiro.De(93.53m, Moeda.EUR), 1.0692m), Agora);

        Should.Throw<ExcecaoTransferenciaJaConcluida>(() => conta.EstornarTransferencia(transferenciaId, Agora));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Conversao_ComTaxaNaoPositiva_LancaExcecaoValorInvalido(int taxa)
    {
        Should.Throw<ExcecaoValorInvalido>(() => Conversao.Criar(Dinheiro.De(1m, Moeda.USD), taxa));
    }
}
