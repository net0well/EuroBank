namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Comando para Contas (chave: a conta de destino): creditar o valor convertido, na moeda do destino.</summary>
public sealed record CreditarTransferenciaCambioMensagem(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    string Moeda,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    string MoedaOrigem,
    decimal ValorOrigem,
    decimal Taxa);
