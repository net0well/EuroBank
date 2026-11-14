namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Os outros eventos finais de transferência: conhecidos, mas sem efeito no antifraude (por enquanto).</summary>
public sealed record EventoTransferenciaSemEfeito(Guid TransferenciaId);
