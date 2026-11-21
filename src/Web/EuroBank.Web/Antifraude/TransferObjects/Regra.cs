namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Uma regra do motor, com peso, ativação, modo sombra e parâmetros.</summary>
public sealed record Regra(
    string Codigo,
    string Tipo,
    string Descricao,
    int Peso,
    string? Efeito,
    bool Ativa,
    bool Sombra,
    IReadOnlyDictionary<string, decimal> Parametros,
    DateTimeOffset AtualizadaEm);
