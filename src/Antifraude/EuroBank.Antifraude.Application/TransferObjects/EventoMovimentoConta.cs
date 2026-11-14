namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// Qualquer outro evento que mexe no saldo (depósito, saque, crédito recebido, estorno, devolução, reconciliação):
/// para o perfil, só o saldo depois do evento importa.
/// </summary>
public sealed record EventoMovimentoConta(Guid ContaId, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm);
