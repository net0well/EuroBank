namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoTransferenciaCambioEnviada(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeDestino,
    string? Descricao,
    decimal Valor,
    decimal Tarifa,
    string MoedaDestino,
    decimal ValorDestino,
    decimal Cotacao,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
