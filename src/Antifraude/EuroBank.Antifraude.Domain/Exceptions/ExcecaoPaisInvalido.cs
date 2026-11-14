using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>País de risco fora do formato ISO de duas letras.</summary>
public sealed class ExcecaoPaisInvalido(string codigo)
    : ExcecaoValidacao("pais_invalido", $"País inválido: '{codigo}' (use o código ISO de duas letras).");
