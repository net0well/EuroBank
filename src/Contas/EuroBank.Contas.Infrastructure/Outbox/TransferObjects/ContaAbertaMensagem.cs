namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para ContaAberta. Moeda é a moeda da conta, válida para todos os eventos dela.</summary>
public sealed record ContaAbertaMensagem(
    Guid ContaId, string Iban, string Titular, string Moeda, int Versao, DateTimeOffset OcorridoEm);
