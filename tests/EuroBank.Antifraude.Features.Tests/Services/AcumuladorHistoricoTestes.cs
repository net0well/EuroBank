using EuroBank.Antifraude.Features.Services;

namespace EuroBank.Antifraude.Features.Tests.Services;

/// <summary>O acumulador do treino segue as mesmas regras do perfil no Redis: versão, retenção e beneficiários.</summary>
public sealed class AcumuladorHistoricoTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VersaoRepetidaOuAntiga_NaoMudaNada()
    {
        var acumulador = new AcumuladorHistorico();
        acumulador.Abrir(1, Inicio).ShouldBeTrue();
        acumulador.Entrada(2, Inicio.AddHours(1), 500m, 500m).ShouldBeTrue();

        acumulador.Entrada(2, Inicio.AddHours(2), 500m, 1_000m).ShouldBeFalse();
        acumulador.Movimento(1, 0m).ShouldBeFalse();

        var historico = acumulador.Retrato(Inicio.AddHours(3));
        historico.Saldo.ShouldBe(500m);
        historico.Entradas.ShouldHaveSingleItem();
    }

    [Fact]
    public void Saida_EntraNoHistoricoENosBeneficiarios()
    {
        var acumulador = new AcumuladorHistorico();
        acumulador.Abrir(1, Inicio);
        acumulador.Saida(2, Inicio.AddDays(1), 80m, "DE89370400440532013000", 420m);

        var historico = acumulador.Retrato(Inicio.AddDays(2));

        historico.Saidas.ShouldHaveSingleItem().Valor.ShouldBe(80m);
        historico.Beneficiarios.ShouldContain("DE89370400440532013000");
        historico.Saldo.ShouldBe(420m);
        historico.AbertaEm.ShouldBe(Inicio);
    }

    [Fact]
    public void RetencaoDe31Dias_NaGravacaoENaLeitura()
    {
        var acumulador = new AcumuladorHistorico();
        acumulador.Abrir(1, Inicio);
        acumulador.Saida(2, Inicio, 10m, "DE89370400440532013000", 90m);
        acumulador.Saida(3, Inicio.AddDays(20), 20m, "DE89370400440532013000", 70m);

        acumulador.Retrato(Inicio.AddDays(30)).Saidas.Count.ShouldBe(2);
        acumulador.Retrato(Inicio.AddDays(40)).Saidas.ShouldHaveSingleItem().Valor.ShouldBe(20m);

        // A saída nova apaga as que passaram da retenção, como o script do Redis.
        acumulador.Saida(4, Inicio.AddDays(32), 30m, "DE89370400440532013000", 40m);
        acumulador.Retrato(Inicio.AddDays(1)).Saidas.Select(saida => saida.Valor).ShouldBe([20m, 30m]);
        acumulador.Retrato(Inicio.AddDays(1)).Beneficiarios.ShouldHaveSingleItem();
    }
}
