namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoTransferenciaCambioRecebida(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanOrigem,
    string NomeOrigem,
    string? Descricao,
    decimal Valor,
    string MoedaOrigem,
    decimal ValorOrigem,
    decimal Cotacao,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
