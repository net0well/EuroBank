using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>O analista se identifica pelo nome em cada ação.</summary>
public sealed class ExcecaoAnalistaObrigatorio()
    : ExcecaoValidacao("analista_obrigatorio", "Informe o nome do analista.");
