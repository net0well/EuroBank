namespace EuroBank.Shared.Persistence.Outbox;

/// <summary>
/// Mensagens que não nascem de um evento de agregado (ex.: uma resposta "débito recusado" de uma saga, quando
/// nada mudou na conta) mas precisam sair pela outbox na mesma transação. Registrada como scoped.
/// </summary>
public interface IFilaOutbox
{
    void Enfileirar(MensagemOutbox mensagem);

    /// <summary>Devolve as mensagens enfileiradas, na ordem, e esvazia a fila.</summary>
    IReadOnlyList<MensagemOutbox> Retirar();
}
