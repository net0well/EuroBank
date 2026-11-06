namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoTransferenciaEnviada(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    decimal Valor,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
