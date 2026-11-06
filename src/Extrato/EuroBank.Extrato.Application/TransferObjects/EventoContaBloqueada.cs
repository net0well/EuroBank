namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoContaBloqueada(Guid ContaId, int Versao, DateTimeOffset OcorridoEm);
