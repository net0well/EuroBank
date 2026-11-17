namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Os outros eventos do antifraude (CasoAberto): conhecidos, sem efeito no perfil.</summary>
public sealed record EventoAntifraudeSemEfeito(Guid OperacaoId);
