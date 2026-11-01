namespace EuroBank.Contas.Infrastructure.Outbox;

/// <summary>
/// Linha da tabela outbox. É gravada na mesma transação da alteração da conta; o Debezium lê o INSERT no WAL
/// e o Outbox Event Router publica o <see cref="Payload"/> no tópico eurobank.{TipoAgregado}.eventos, com
/// chave <see cref="IdAgregado"/> e os headers id, tipo e traceparent.
/// </summary>
public sealed class MensagemOutbox
{
    public MensagemOutbox(
        Guid id,
        string tipoAgregado,
        string idAgregado,
        string tipoEvento,
        string payload,
        DateTimeOffset ocorridoEm,
        string? traceparent)
    {
        Id = id;
        TipoAgregado = tipoAgregado;
        IdAgregado = idAgregado;
        TipoEvento = tipoEvento;
        Payload = payload;
        OcorridoEm = ocorridoEm;
        Traceparent = traceparent;
    }

    /// <summary>Id do evento; vira o header "id" da mensagem e é a chave de deduplicação no Extrato.</summary>
    public Guid Id { get; private set; }

    /// <summary>Define o tópico de destino (eurobank.contas.eventos).</summary>
    public string TipoAgregado { get; private set; }

    /// <summary>Id da conta; vira a chave da mensagem, garantindo ordem por conta na partição.</summary>
    public string IdAgregado { get; private set; }

    /// <summary>Nome do evento (ex.: DepositoRealizado); vira o header "tipo".</summary>
    public string TipoEvento { get; private set; }

    /// <summary>Evento de integração serializado em JSON (coluna jsonb).</summary>
    public string Payload { get; private set; }

    public DateTimeOffset OcorridoEm { get; private set; }

    /// <summary>Contexto W3C da requisição que gerou o evento, para o trace continuar no Extrato.</summary>
    public string? Traceparent { get; private set; }

    /// <summary>
    /// Ordem física de inserção (identity gerada pelo banco). É a ordem do WAL, logo a ordem em que o
    /// Debezium publica; serve para conferir e inspecionar essa ordem.
    /// </summary>
    public long Sequencia { get; private set; }
}
