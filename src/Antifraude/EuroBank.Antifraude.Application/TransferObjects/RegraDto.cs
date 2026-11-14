namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Uma regra como está configurada agora. Efeito só nas regras duras (Recusar ou Revisar).</summary>
public sealed record RegraDto(
    string Codigo,
    string Tipo,
    string Descricao,
    int Peso,
    string? Efeito,
    bool Ativa,
    bool Sombra,
    IReadOnlyDictionary<string, decimal> Parametros,
    DateTimeOffset AtualizadaEm);
