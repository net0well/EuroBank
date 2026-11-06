namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoSaqueRealizado(
    Guid ContaId, decimal Valor, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm);
