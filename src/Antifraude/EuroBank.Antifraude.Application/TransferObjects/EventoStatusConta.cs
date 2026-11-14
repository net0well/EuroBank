namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>ContaBloqueada, ContaDesbloqueada e ContaEncerrada: o perfil só avança a versão da conta.</summary>
public sealed record EventoStatusConta(Guid ContaId, int Versao, DateTimeOffset OcorridoEm);
