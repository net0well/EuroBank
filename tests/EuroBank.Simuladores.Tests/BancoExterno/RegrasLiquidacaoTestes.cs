using EuroBank.Simuladores.BancoExterno.Regras;
using Microsoft.Extensions.Options;

namespace EuroBank.Simuladores.Tests.BancoExterno;

public sealed class RegrasLiquidacaoTestes
{
    private static RegrasLiquidacao Regras(double taxaRecusa = 0, double taxaSilencio = 0) => new(
        Options.Create(new OpcoesBancoExterno
        {
            LatenciaMs = 1000,
            AtrasoFinal97Segundos = 45,
            AtrasoFinal95Segundos = 300,
            AtrasoDevolucaoSegundos = 10,
            TaxaRecusa = taxaRecusa,
            TaxaSilencio = taxaSilencio,
        }),
        new Random(42));

    [Theory]
    [InlineData("DE89370400440532013099", ComportamentoLiquidacao.Recusar, 1)]
    [InlineData("DE89370400440532013098", ComportamentoLiquidacao.Silenciar, 0)]
    [InlineData("DE89370400440532013097", ComportamentoLiquidacao.Confirmar, 45)]
    [InlineData("DE89370400440532013096", ComportamentoLiquidacao.LiquidarSemResponder, 0)]
    [InlineData("DE89370400440532013095", ComportamentoLiquidacao.ConfirmarDepoisDeNegar, 300)]
    [InlineData("DE89370400440532013094", ComportamentoLiquidacao.ConfirmarEDevolver, 1)]
    [InlineData("DE89370400440532013000", ComportamentoLiquidacao.Confirmar, 1)]
    public void FinalDoIban_DefineOComportamento(string iban, ComportamentoLiquidacao esperado, int segundos)
    {
        var decisao = Regras().Decidir(iban);

        decisao.Comportamento.ShouldBe(esperado);
        decisao.Atraso.ShouldBe(TimeSpan.FromSeconds(segundos));
    }

    [Fact]
    public void TaxaDeRecusaTotal_RecusaIbanComum()
    {
        Regras(taxaRecusa: 1).Decidir("DE89370400440532013000").Comportamento.ShouldBe(ComportamentoLiquidacao.Recusar);
    }

    [Fact]
    public void TaxaDeSilencioTotal_SilenciaIbanComum()
    {
        Regras(taxaSilencio: 1).Decidir("DE89370400440532013000").Comportamento.ShouldBe(ComportamentoLiquidacao.Silenciar);
    }

    [Fact]
    public void RegraDeterministica_IgnoraAsTaxasAleatorias()
    {
        Regras(taxaSilencio: 1).Decidir("DE89370400440532013099").Comportamento.ShouldBe(ComportamentoLiquidacao.Recusar);
    }
}
