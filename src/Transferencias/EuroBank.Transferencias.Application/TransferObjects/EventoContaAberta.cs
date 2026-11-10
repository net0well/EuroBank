namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>
/// ContaAberta como Transferencias o lê de eurobank.contas.eventos (tolerant reader): só o que a projeção usa.
/// Eventos antigos não têm moeda: eram todos em euro.
/// </summary>
public sealed record EventoContaAberta(Guid ContaId, string Iban, string Titular, DateTimeOffset OcorridoEm, string? Moeda = null);
