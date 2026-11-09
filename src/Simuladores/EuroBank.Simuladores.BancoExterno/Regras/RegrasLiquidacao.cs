using Microsoft.Extensions.Options;

namespace EuroBank.Simuladores.BancoExterno.Regras;

/// <summary>
/// Decide como o banco externo reage pelo final do IBAN de destino. As regras fixas existem para os testes e os
/// exercícios reproduzirem cada caminho da saga; as taxas aleatórias valem só para os demais IBANs (ex.: no k6).
/// </summary>
public sealed class RegrasLiquidacao(IOptions<OpcoesBancoExterno> opcoes, Random aleatorio)
{
    /// <summary>Tempo até responder uma liquidação ou consulta comum.</summary>
    public TimeSpan Latencia => TimeSpan.FromMilliseconds(opcoes.Value.LatenciaMs);

    public DecisaoLiquidacao Decidir(string ibanDestino)
    {
        var configuracao = opcoes.Value;
        var latencia = Latencia;
        var final = ibanDestino.Length >= 2 ? ibanDestino[^2..] : string.Empty;

        return final switch
        {
            "99" => new DecisaoLiquidacao(ComportamentoLiquidacao.Recusar, latencia),
            "98" => new DecisaoLiquidacao(ComportamentoLiquidacao.Silenciar, TimeSpan.Zero),
            "97" => new DecisaoLiquidacao(ComportamentoLiquidacao.Confirmar, TimeSpan.FromSeconds(configuracao.AtrasoFinal97Segundos)),
            "96" => new DecisaoLiquidacao(ComportamentoLiquidacao.LiquidarSemResponder, TimeSpan.Zero),
            _ => Sortear(configuracao, latencia),
        };
    }

    private DecisaoLiquidacao Sortear(OpcoesBancoExterno configuracao, TimeSpan latencia)
    {
        var sorteio = aleatorio.NextDouble();
        if (sorteio < configuracao.TaxaSilencio)
        {
            return new DecisaoLiquidacao(ComportamentoLiquidacao.Silenciar, TimeSpan.Zero);
        }

        return sorteio < configuracao.TaxaSilencio + configuracao.TaxaRecusa
            ? new DecisaoLiquidacao(ComportamentoLiquidacao.Recusar, latencia)
            : new DecisaoLiquidacao(ComportamentoLiquidacao.Confirmar, latencia);
    }
}
