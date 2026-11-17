using System.Text.Json;
using System.Text.Json.Serialization;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Conversions;

/// <summary>
/// Colunas jsonb: os sinais da análise (um retrato que só se lê inteiro) e os parâmetros das regras. Enums como
/// texto, para o jsonb ficar legível no psql. Os comparadores deixam o EF perceber mudança dentro do dicionário.
/// </summary>
public static class ConversoesJson
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static ValueConverter<List<SinalRegra>, string> Sinais { get; } = new(
        sinais => JsonSerializer.Serialize(sinais, Json),
        json => JsonSerializer.Deserialize<List<SinalRegra>>(json, Json) ?? new List<SinalRegra>());

    public static ValueComparer<List<SinalRegra>> ComparadorSinais { get; } = new(
        (esquerda, direita) => esquerda!.SequenceEqual(direita!),
        sinais => sinais.Aggregate(0, (hash, sinal) => HashCode.Combine(hash, sinal.GetHashCode())),
        sinais => sinais.ToList());

    /// <summary>As features com os nomes de cada campo, para o jsonb ser lido sem saber a ordem do vetor.</summary>
    public static ValueConverter<VetorFeatures?, string?> Features { get; } = new(
        features => features == null ? null : JsonSerializer.Serialize(features, Json),
        json => json == null ? null : JsonSerializer.Deserialize<VetorFeatures>(json, Json));

    /// <summary>A pontuação do modelo numa análise (ativa ou challenger), com a explicação; nula sem modelo.</summary>
    public static ValueConverter<PontuacaoModelo?, string?> Pontuacao { get; } = new(
        pontuacao => pontuacao == null ? null : JsonSerializer.Serialize(pontuacao, Json),
        json => json == null ? null : JsonSerializer.Deserialize<PontuacaoModelo>(json, Json));

    public static ValueConverter<MetricasModelo, string> Metricas { get; } = new(
        metricas => JsonSerializer.Serialize(metricas, Json),
        json => JsonSerializer.Deserialize<MetricasModelo>(json, Json)!);

    public static ValueConverter<DadosTreino, string> DadosTreino { get; } = new(
        dados => JsonSerializer.Serialize(dados, Json),
        json => JsonSerializer.Deserialize<DadosTreino>(json, Json)!);

    public static ValueConverter<List<DistribuicaoFeature>, string> Distribuicao { get; } = new(
        distribuicao => JsonSerializer.Serialize(distribuicao, Json),
        json => JsonSerializer.Deserialize<List<DistribuicaoFeature>>(json, Json) ?? new List<DistribuicaoFeature>());

    public static ValueComparer<List<DistribuicaoFeature>> ComparadorDistribuicao { get; } = new(
        (esquerda, direita) => JsonSerializer.Serialize(esquerda, Json) == JsonSerializer.Serialize(direita, Json),
        distribuicao => JsonSerializer.Serialize(distribuicao, Json).GetHashCode(StringComparison.Ordinal),
        distribuicao => distribuicao.ToList());

    public static ValueConverter<List<PsiFeature>, string> Psis { get; } = new(
        psis => JsonSerializer.Serialize(psis, Json),
        json => JsonSerializer.Deserialize<List<PsiFeature>>(json, Json) ?? new List<PsiFeature>());

    public static ValueComparer<List<PsiFeature>> ComparadorPsis { get; } = new(
        (esquerda, direita) => esquerda!.SequenceEqual(direita!),
        psis => psis.Aggregate(0, (hash, psi) => HashCode.Combine(hash, psi.GetHashCode())),
        psis => psis.ToList());

    public static ValueConverter<Dictionary<string, decimal>, string> Parametros { get; } = new(
        parametros => JsonSerializer.Serialize(parametros, Json),
        json => JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, Json) ?? new Dictionary<string, decimal>());

    public static ValueComparer<Dictionary<string, decimal>> ComparadorParametros { get; } = new(
        (esquerda, direita) => MesmosParametros(esquerda!, direita!),
        parametros => parametros.OrderBy(par => par.Key, StringComparer.Ordinal)
            .Aggregate(0, (hash, par) => HashCode.Combine(hash, par.Key, par.Value)),
        parametros => new Dictionary<string, decimal>(parametros, StringComparer.Ordinal));

    // Fora da expressão do comparador: árvores de expressão não aceitam variável declarada em "out".
    private static bool MesmosParametros(Dictionary<string, decimal> esquerda, Dictionary<string, decimal> direita) =>
        esquerda.Count == direita.Count
        && esquerda.All(par => direita.TryGetValue(par.Key, out var valor) && valor == par.Value);
}
