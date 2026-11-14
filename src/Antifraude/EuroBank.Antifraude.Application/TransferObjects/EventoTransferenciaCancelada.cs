namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>TransferenciaCancelada (eurobank.transferencias.eventos): o antifraude só olha o id e o motivo.</summary>
public sealed record EventoTransferenciaCancelada(Guid TransferenciaId, Guid ContaOrigemId, string Motivo, DateTimeOffset OcorridoEm);
