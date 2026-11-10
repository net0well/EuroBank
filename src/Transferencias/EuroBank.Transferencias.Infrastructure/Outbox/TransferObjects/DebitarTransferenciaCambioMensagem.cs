namespace EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

/// <summary>Comando para Contas: debitar a origem de um câmbio interno, na moeda dela.</summary>
public sealed record DebitarTransferenciaCambioMensagem(
    Guid TransferenciaId,
    Guid ContaId,
    decimal Valor,
    decimal Tarifa,
    string Moeda,
    string IbanDestino,
    string NomeDestino,
    string? Descricao,
    string MoedaDestino,
    decimal ValorDestino,
    decimal Taxa);
