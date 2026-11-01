namespace EuroBank.Shared.Exceptions;

public sealed class ExcecaoPaginacaoInvalida(string motivo)
    : ExcecaoValidacao("paginacao_invalida", $"Paginação inválida: {motivo}.");
