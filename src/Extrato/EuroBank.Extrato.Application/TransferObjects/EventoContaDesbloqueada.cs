namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record EventoContaDesbloqueada(Guid ContaId, int Versao, DateTimeOffset OcorridoEm);
