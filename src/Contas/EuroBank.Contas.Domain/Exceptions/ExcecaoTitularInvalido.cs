using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTitularInvalido(string motivo)
    : ExcecaoValidacao("titular_invalido", $"Titular inválido: {motivo}.");
