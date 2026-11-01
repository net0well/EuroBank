using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;

namespace EuroBank.Contas.Domain.Events;

/// <summary>
/// Evento de domínio de uma conta. <see cref="Versao"/> é a versão da conta depois do evento: cada operação
/// incrementa a versão em 1, o que permite ao Extrato descartar eventos repetidos.
/// </summary>
public interface IEventoDeConta : IEventoDominio
{
    ContaId ContaId { get; }

    int Versao { get; }
}
