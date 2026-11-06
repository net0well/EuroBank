namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Valores chegam como string decimal ("100.00") e são lidos como decimal.</summary>
public sealed record EventoDepositoRealizado(
    Guid ContaId, decimal Valor, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm);
