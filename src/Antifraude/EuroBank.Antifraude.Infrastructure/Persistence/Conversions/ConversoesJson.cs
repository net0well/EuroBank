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
