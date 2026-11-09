using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Events;

/// <summary>
/// O que a saga decidiu. Os eventos terminados em "Solicitado" viram comandos para os participantes; os demais
/// contam o fim da transferência. A infraestrutura escolhe o tópico de cada um na outbox.
/// </summary>
public interface IEventoDeTransferencia : IEventoDominio
{
    TransferenciaId TransferenciaId { get; }
}
