namespace EuroBank.Antifraude.Api.TransferObjects;

/// <summary>Parametros: só os que mudam (os outros ficam como estão).</summary>
public sealed record AjusteRegraRequisicao(
    int Peso,
    bool Ativa,
    bool Sombra,
    Dictionary<string, decimal>? Parametros = null);
