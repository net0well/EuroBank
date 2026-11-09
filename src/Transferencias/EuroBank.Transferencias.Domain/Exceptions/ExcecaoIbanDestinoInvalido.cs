using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoIbanDestinoInvalido(string valor)
    : ExcecaoValidacao("iban_destino_invalido", $"IBAN de destino inválido: '{valor}'.");
