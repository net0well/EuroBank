namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoDevolucaoTransferenciaExterna(
    Guid ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    decimal Valor,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
