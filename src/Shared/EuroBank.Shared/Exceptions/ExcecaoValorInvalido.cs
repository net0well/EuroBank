namespace EuroBank.Shared.Exceptions;

public sealed class ExcecaoValorInvalido(string motivo)
    : ExcecaoValidacao("valor_invalido", $"Valor inválido: {motivo}.");
