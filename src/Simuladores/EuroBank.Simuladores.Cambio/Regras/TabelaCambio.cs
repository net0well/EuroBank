using Microsoft.Extensions.Options;

namespace EuroBank.Simuladores.Cambio.Regras;

/// <summary>
/// Taxas do câmbio simulado, com base no euro: 1 EUR = 1,08 USD = 0,86 GBP = 5,90 BRL. Pares sem euro são
/// cruzados pela base (GBP para USD = 1,08 / 0,86). O spread reduz a taxa, sempre contra o cliente. A taxa fica
/// com 6 casas; os valores com 2, arredondados meio para cima.
/// </summary>
public sealed class TabelaCambio(IOptions<OpcoesCambio> opcoes)
{
    private static readonly Dictionary<string, decimal> PorEuro = new(StringComparer.Ordinal)
    {
        ["EUR"] = 1.00m,
        ["USD"] = 1.08m,
        ["GBP"] = 0.86m,
        ["BRL"] = 5.90m,
    };

    /// <summary>Pares que o mercado simulado não negocia (em qualquer sentido).</summary>
    private static readonly HashSet<(string, string)> Indisponiveis = [("BRL", "GBP"), ("GBP", "BRL")];

    public ResultadoCotacao Cotar(string moedaOrigem, string moedaDestino, decimal valor, string moedaDoValor)
    {
        if (!PorEuro.TryGetValue(moedaOrigem, out var origem) || !PorEuro.TryGetValue(moedaDestino, out var destino))
        {
            return ResultadoCotacao.Indisponivel($"moeda não negociada ({moedaOrigem} para {moedaDestino})");
        }

        if (moedaOrigem == moedaDestino)
        {
            return ResultadoCotacao.Indisponivel("as moedas de origem e de destino são iguais");
        }

        if (Indisponiveis.Contains((moedaOrigem, moedaDestino)))
        {
            return ResultadoCotacao.Indisponivel($"par {moedaOrigem}/{moedaDestino} indisponível");
        }

        var taxa = decimal.Round(destino / origem * (1m - opcoes.Value.Spread), 6, MidpointRounding.AwayFromZero);

        if (moedaDoValor == moedaOrigem)
        {
            return new ResultadoCotacao(true, taxa, valor, Arredondar(valor * taxa), null);
        }

        return moedaDoValor == moedaDestino
            ? new ResultadoCotacao(true, taxa, Arredondar(valor / taxa), valor, null)
            : ResultadoCotacao.Indisponivel("o valor precisa estar na moeda de origem ou na de destino");
    }

    private static decimal Arredondar(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
