namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoTransferenciaExternaEnviada(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    decimal Valor,
    decimal Tarifa,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
