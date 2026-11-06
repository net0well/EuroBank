namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoTransferenciaRecebida(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanOrigem,
    decimal Valor,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
