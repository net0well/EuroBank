using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.ValueObjects;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaSepaTestes
{
    // 2026-11-13 é uma sexta-feira.
    private static readonly DateTimeOffset SextaAs17 = new(2026, 11, 13, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SegundaAs8 = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);

    private static Transferencia Externa(
        DateTimeOffset agora,
        DateTimeOffset? executarEm = null,
        string iban = IbanComum,
        string? bic = "DEUTDEFF",
        Moeda moedaDestino = Moeda.EUR,
        PoliticaSepa? sepa = null) =>
        Transferencia.SolicitarExterna(
            Origem,
            IbanDestino.Criar(iban),
            bic,
            "Bruno",
            Dinheiro.De(100m),
            moedaDestino,
            null,
            null,
            "chave",
            "hash",
            executarEm,
            agora,
            sepa ?? PoliticaSepa.Padrao);

    [Theory]
    [InlineData("DEUT")]
    [InlineData("1234DEFF")]
    [InlineData("DEUTDEFF5")]
    [InlineData("DEUT-DEFF")]
    public void BicForaDoFormato_Recusa(string bic)
    {
        Should.Throw<ExcecaoBicInvalido>(() => Externa(Agora, bic: bic));
    }

    [Theory]
    [InlineData("deutdeff", "DEUTDEFF")]
    [InlineData("DEUTDEFF500", "DEUTDEFF500")]
    public void BicDe8Ou11Caracteres_Aceita(string bic, string normalizado)
    {
        Externa(Agora, bic: bic).Bic.ShouldBe(normalizado);
    }

    [Fact]
    public void EmEuroParaForaDaSepa_Recusa()
    {
        Should.Throw<ExcecaoPaisForaDaSepa>(() => Externa(Agora, iban: "US64SVBKUS6S3300958879", bic: "SVBKUS6S"));
    }

    [Fact]
    public void EmDolarParaForaDaSepa_Aceita()
    {
        Externa(Agora, iban: "US64SVBKUS6S3300958879", bic: "SVBKUS6S", moedaDestino: Moeda.USD)
            .MoedaDestino.ShouldBe(Moeda.USD);
    }

    [Fact]
    public void EmEuroNaSextaDepoisDoCorte_VaiParaSegundaAs8()
    {
        var transferencia = Externa(SextaAs17);

        transferencia.ExecutarEmEfetivo.ShouldBe(SegundaAs8);
        transferencia.EstaNaHora(SextaAs17).ShouldBeFalse();
    }

    [Theory]
    [InlineData("2026-11-14T10:00:00Z", "2026-11-16T08:00:00Z")] // sábado
    [InlineData("2026-11-15T23:59:00Z", "2026-11-16T08:00:00Z")] // domingo
    [InlineData("2026-11-09T16:00:00Z", "2026-11-10T08:00:00Z")] // segunda, no corte
    [InlineData("2026-11-09T15:59:00Z", "2026-11-09T15:59:00Z")] // segunda, antes do corte
    [InlineData("2026-11-10T06:00:00Z", "2026-11-10T06:00:00Z")] // terça cedo
    public void HorarioDeCorte_DeslocaParaOProximoDiaUtil(string pedido, string efetivo)
    {
        var agora = DateTimeOffset.Parse(pedido, System.Globalization.CultureInfo.InvariantCulture);

        Externa(agora).ExecutarEmEfetivo.ShouldBe(DateTimeOffset.Parse(efetivo, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AgendadaParaUmSabado_VaiParaSegundaAs8()
    {
        Externa(Agora, executarEm: new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero))
            .ExecutarEmEfetivo.ShouldBe(SegundaAs8);
    }

    [Fact]
    public void EmOutraMoeda_NaoTemHorarioDeCorte()
    {
        Externa(SextaAs17, iban: "US64SVBKUS6S3300958879", bic: "SVBKUS6S", moedaDestino: Moeda.USD)
            .ExecutarEmEfetivo.ShouldBe(SextaAs17);
    }

    [Fact]
    public void CorteDesligado_ExecutaNaHora()
    {
        Externa(SextaAs17, sepa: PoliticaSepa.Padrao with { AplicarHorarioDeCorte = false })
            .ExecutarEmEfetivo.ShouldBe(SextaAs17);
    }

    [Fact]
    public void CambioInterno_NaoTemHorarioDeCorte()
    {
        Transferencia.SolicitarCambioInterno(
                Origem, DestinoEmDolar, Dinheiro.De(10m, Moeda.USD), null, null, "k", "h", null, SextaAs17)
            .ExecutarEmEfetivo.ShouldBe(SextaAs17);
    }
}
