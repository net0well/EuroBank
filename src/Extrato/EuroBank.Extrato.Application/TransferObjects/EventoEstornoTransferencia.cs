namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Compensação da saga: <see cref="Valor"/> é o total devolvido (valor + tarifa).</summary>
public sealed record EventoEstornoTransferencia(
    Guid ContaId,
    Guid TransferenciaId,
    string NomeBeneficiario,
    decimal Valor,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
