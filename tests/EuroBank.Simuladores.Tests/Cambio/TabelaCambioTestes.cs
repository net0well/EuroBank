using EuroBank.Simuladores.Cambio.Regras;
using Microsoft.Extensions.Options;

namespace EuroBank.Simuladores.Tests.Cambio;

public sealed class TabelaCambioTestes
{
    private readonly TabelaCambio _tabela = new(Options.Create(new OpcoesCambio { Spread = 0.01m }));

    [Fact]
    public void EurParaUsd_AplicaOSpreadAFavorDoBanco()
    {
        var cotacao = _tabela.Cotar("EUR", "USD", 100m, "EUR");

        cotacao.Disponivel.ShouldBeTrue();
        cotacao.Taxa.ShouldBe(1.0692m);
        cotacao.ValorOrigem.ShouldBe(100m);
        cotacao.ValorDestino.ShouldBe(106.92m);
    }

    [Fact]
    public void ValorNaMoedaDeDestino_CalculaQuantoAOrigemPaga()
    {
        var cotacao = _tabela.Cotar("EUR", "USD", 100m, "USD");

        cotacao.ValorDestino.ShouldBe(100m);
        cotacao.ValorOrigem.ShouldBe(93.53m);
    }

    [Fact]
    public void ParCruzado_PassaPeloEuro()
    {
        var cotacao = _tabela.Cotar("GBP", "USD", 100m, "GBP");

        cotacao.Taxa.ShouldBe(1.243256m);
        cotacao.ValorDestino.ShouldBe(124.33m);
    }

    [Fact]
    public void VoltaParaOEuro_UsaATaxaInversaComSpread()
    {
        // 1 / 1,08 x 0,99 = 0,9166666... (6 casas).
        _tabela.Cotar("USD", "EUR", 100m, "USD").Taxa.ShouldBe(0.916667m);
    }

    [Fact]
    public void Arredondamento_MeioParaCima()
    {
        // 25 x 0,8514 = 21,285: meio exato, vai para cima (o arredondamento bancário daria 21,28).
        _tabela.Cotar("EUR", "GBP", 25m, "EUR").ValorDestino.ShouldBe(21.29m);
    }

    [Theory]
    [InlineData("BRL", "GBP", "BRL")]
    [InlineData("GBP", "BRL", "GBP")]
    public void ParBrlGbp_Indisponivel(string origem, string destino, string moedaDoValor)
    {
        var cotacao = _tabela.Cotar(origem, destino, 100m, moedaDoValor);

        cotacao.Disponivel.ShouldBeFalse();
        cotacao.Motivo.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("EUR", "EUR", "EUR")]
    [InlineData("EUR", "JPY", "EUR")]
    [InlineData("EUR", "USD", "GBP")]
    public void PedidoSemCotacao_Indisponivel(string origem, string destino, string moedaDoValor)
    {
        _tabela.Cotar(origem, destino, 100m, moedaDoValor).Disponivel.ShouldBeFalse();
    }
}
