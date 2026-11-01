using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoIbanInvalido(string valor)
    : ExcecaoValidacao("iban_invalido", $"IBAN inválido: '{valor}'.");
