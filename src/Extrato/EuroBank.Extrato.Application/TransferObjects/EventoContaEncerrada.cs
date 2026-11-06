namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoContaEncerrada(Guid ContaId, int Versao, DateTimeOffset OcorridoEm);
