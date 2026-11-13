namespace EuroBank.Transferencias.Application.Replies;

/// <summary>
/// O banco externo devolveu uma transferência já liquidada (no tópico, tipo TransferenciaDevolvida). Valor e Moeda são
/// os da liquidação; Contas credita de volta o que tinha debitado, na moeda da conta.
/// </summary>
public sealed record DevolucaoRecebida(Guid TransferenciaId, decimal Valor, string Moeda, string Motivo);
