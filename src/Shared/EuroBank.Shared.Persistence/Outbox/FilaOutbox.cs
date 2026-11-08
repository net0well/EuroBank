namespace EuroBank.Shared.Persistence.Outbox;

public sealed class FilaOutbox : IFilaOutbox
{
    private readonly List<MensagemOutbox> _mensagens = [];

    public void Enfileirar(MensagemOutbox mensagem) => _mensagens.Add(mensagem);

    public IReadOnlyList<MensagemOutbox> Retirar()
    {
        var retiradas = _mensagens.ToList();
        _mensagens.Clear();
        return retiradas;
    }
}
