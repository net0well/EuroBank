namespace EuroBank.Shared.Exceptions;

public sealed class ExcecaoDispositivoInvalido()
    : ExcecaoValidacao("dispositivo_invalido", "O id do dispositivo tem no máximo 100 caracteres.");
