using EuroBank.Shared.Exceptions;

namespace EuroBank.Extrato.Application.Exceptions;

/// <summary>A mensagem não pode ser processada; o Wolverine a move direto para o dead letter.</summary>
public sealed class ExcecaoMensagemInvalida(string motivo)
    : ExcecaoValidacao("mensagem_invalida", $"Mensagem inválida: {motivo}.");
