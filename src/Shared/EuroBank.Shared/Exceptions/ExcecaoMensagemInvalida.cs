
namespace EuroBank.Shared.Exceptions;

/// <summary>A mensagem não pode ser processada; o Wolverine a move direto para o dead letter.</summary>
public sealed class ExcecaoMensagemInvalida(string motivo)
    : ExcecaoValidacao("mensagem_invalida", $"Mensagem inválida: {motivo}.");
