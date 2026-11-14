using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Outro analista assumiu o caso.</summary>
public sealed class ExcecaoCasoDeOutroAnalista(string analistaAtual)
    : ExcecaoConflito("caso_de_outro_analista", $"O caso está com o analista {analistaAtual}.");
