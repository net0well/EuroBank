namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>ContaEncerrada como Transferencias o lê de eurobank.contas.eventos (tolerant reader).</summary>
public sealed record EventoContaEncerrada(Guid ContaId, DateTimeOffset OcorridoEm);
