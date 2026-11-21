namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Ajustar uma regra (os parâmetros só os que mudam).</summary>
public sealed record AjusteRegraRequisicao(
    int Peso,
    bool Ativa,
    bool Sombra,
    IReadOnlyDictionary<string, decimal>? Parametros);
