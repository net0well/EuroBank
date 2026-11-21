namespace EuroBank.Shared.Exceptions;

public sealed class ExcecaoLocalizacaoInvalida(string detalhe)
    : ExcecaoValidacao("localizacao_invalida", $"Localização inválida: {detalhe}.");
