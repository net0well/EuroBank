namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para ContaAberta.</summary>
public sealed record ContaAbertaMensagem(
    Guid ContaId, string Iban, string Titular, int Versao, DateTimeOffset OcorridoEm);
