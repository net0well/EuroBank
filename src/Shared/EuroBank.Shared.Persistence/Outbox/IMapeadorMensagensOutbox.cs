using EuroBank.Shared.Domain;

namespace EuroBank.Shared.Persistence.Outbox;

/// <summary>
/// Converte um evento de domínio de um contexto na linha da outbox: escolhe o tipo de agregado (o tópico), o id
/// do agregado (a chave) e o payload publicado (o contrato de integração).
/// </summary>
public interface IMapeadorMensagensOutbox
{
    MensagemOutbox Mapear(IEventoDominio evento, string? traceparent);
}
