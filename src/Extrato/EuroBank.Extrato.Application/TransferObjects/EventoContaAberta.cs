namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>
/// Evento de integração ContaAberta como o Extrato o lê (tolerant reader): só os campos que usa, sem referência
/// a nenhum projeto do contexto Contas. Eventos antigos não têm moeda: eram todos em euro.
/// </summary>
public sealed record EventoContaAberta(
    Guid ContaId, string Iban, string Titular, int Versao, DateTimeOffset OcorridoEm, string? Moeda = null);
