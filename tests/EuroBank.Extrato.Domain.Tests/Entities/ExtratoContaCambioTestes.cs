using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Tests.Entities;

public sealed class ExtratoContaCambioTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Abrir_GuardaAMoedaDaConta_ESaldoNessaMoeda()
    {
        var extrato = ExtratoConta.Abrir(Guid.CreateVersion7(), "PT50EURO0000000000002", "Bruno", 1, Agora, Moeda.USD);

        extrato.Moeda.ShouldBe(Moeda.USD);
        extrato.SaldoAtual.ShouldBe(Dinheiro.ZeroEm(Moeda.USD));
    }

    [Fact]
    public void CambioEnviado_RegistraValorTarifaEOQueODestinoRecebeu()
    {
        var extrato = ExtratoConta.Abrir(Guid.CreateVersion7(), "PT50EURO0000000000001", "Ana", 1, Agora);
        extrato.RegistrarDeposito(2, Dinheiro.De(1_000m), Dinheiro.De(1_000m), Agora);

        extrato.RegistrarTransferenciaCambioEnviada(
            3, Guid.CreateVersion7(), "PT50EURO0000000000002", "Bruno", null,
            Dinheiro.De(93.53m), Dinheiro.De(2.50m), Dinheiro.De(903.97m), new CambioDoLancamento(Moeda.USD, 100m, 1.0692m), Agora)
            .ShouldBe(ResultadoAplicacao.Aplicado);

        var lancamento = extrato.Lancamentos[^1];
        lancamento.Tipo.ShouldBe(TipoLancamento.TransferenciaCambioEnviada);
        lancamento.Tarifa.ShouldBe(Dinheiro.De(2.50m));
        lancamento.Cambio.ShouldBe(new CambioDoLancamento(Moeda.USD, 100m, 1.0692m));
        lancamento.Descricao.ShouldContain("Bruno");
        extrato.SaldoAtual.ShouldBe(Dinheiro.De(903.97m));
    }

    [Fact]
    public void CambioRecebido_RegistraOValorNaMoedaDaContaEOQueAOrigemPagou()
    {
        var extrato = ExtratoConta.Abrir(Guid.CreateVersion7(), "PT50EURO0000000000002", "Bruno", 1, Agora, Moeda.USD);

        extrato.RegistrarTransferenciaCambioRecebida(
            2, Guid.CreateVersion7(), "PT50EURO0000000000001", "Ana", null,
            Dinheiro.De(100m, Moeda.USD), Dinheiro.De(100m, Moeda.USD), new CambioDoLancamento(Moeda.EUR, 93.53m, 1.0692m), Agora);

        var lancamento = extrato.Lancamentos.ShouldHaveSingleItem();
        lancamento.Tipo.ShouldBe(TipoLancamento.TransferenciaCambioRecebida);
        lancamento.Valor.ShouldBe(Dinheiro.De(100m, Moeda.USD));
        lancamento.Cambio!.Moeda.ShouldBe(Moeda.EUR);
        extrato.SaldoAtual.ShouldBe(Dinheiro.De(100m, Moeda.USD));
    }

    [Fact]
    public void TransferenciaExternaComCambio_RegistraOQueODestinoRecebeu()
    {
        var extrato = ExtratoConta.Abrir(Guid.CreateVersion7(), "PT50EURO0000000000001", "Ana", 1, Agora);
        extrato.RegistrarDeposito(2, Dinheiro.De(1_000m), Dinheiro.De(1_000m), Agora);

        extrato.RegistrarTransferenciaExternaEnviada(
            3, Guid.CreateVersion7(), "US64SVBKUS6S3300958879", "Bruno", null,
            Dinheiro.De(93.53m), Dinheiro.De(2.50m), Dinheiro.De(903.97m), Agora, new CambioDoLancamento(Moeda.USD, 100m, 1.0692m));

        extrato.Lancamentos[^1].Cambio.ShouldNotBeNull().Valor.ShouldBe(100m);
    }

    [Fact]
    public void CambioEnviadoRepetido_EIgnoradoPelaVersao()
    {
        var extrato = ExtratoConta.Abrir(Guid.CreateVersion7(), "PT50EURO0000000000002", "Bruno", 1, Agora, Moeda.USD);
        var cambio = new CambioDoLancamento(Moeda.EUR, 93.53m, 1.0692m);
        extrato.RegistrarTransferenciaCambioRecebida(
            2, Guid.CreateVersion7(), "PT50EURO0000000000001", "Ana", null, Dinheiro.De(100m, Moeda.USD), Dinheiro.De(100m, Moeda.USD), cambio, Agora);

        extrato.RegistrarTransferenciaCambioRecebida(
            2, Guid.CreateVersion7(), "PT50EURO0000000000001", "Ana", null, Dinheiro.De(100m, Moeda.USD), Dinheiro.De(100m, Moeda.USD), cambio, Agora)
            .ShouldBe(ResultadoAplicacao.IgnoradoJaAplicado);

        extrato.Lancamentos.Count.ShouldBe(1);
    }
}
