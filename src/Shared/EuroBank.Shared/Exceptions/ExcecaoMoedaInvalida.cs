namespace EuroBank.Shared.Exceptions;

public sealed class ExcecaoMoedaInvalida(string codigo)
    : ExcecaoValidacao("moeda_invalida", $"Moeda '{codigo}' inválida: use EUR, USD, GBP ou BRL.");
