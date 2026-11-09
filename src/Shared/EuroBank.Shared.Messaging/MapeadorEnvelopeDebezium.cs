using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EuroBank.Shared.TransferObjects;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Util;

namespace EuroBank.Shared.Messaging;

/// <summary>
/// Traduz as mensagens que o Outbox Event Router do Debezium publica em envelopes do Wolverine:
/// <list type="bullet">
/// <item>header <c>tipo</c>: escolhe o tipo .NET da mensagem e, com ele, o manipulador;</item>
/// <item>header <c>id</c> (id do evento na outbox): vira o id do envelope, chave de deduplicação da inbox durável;</item>
/// <item>header <c>traceparent</c>: vira o pai da Activity, continuando o trace iniciado por quem publicou;</item>
/// <item>corpo: o JSON do evento, desserializado no DTO do consumidor.</item>
/// </list>
/// Cada consumidor informa a própria tabela "tipo publicado para tipo .NET" (tolerant reader).
/// </summary>
public sealed class MapeadorEnvelopeDebezium(IReadOnlyDictionary<string, Type> tiposPorEvento) : IKafkaEnvelopeMapper
{
    private readonly Dictionary<string, string> _tiposPorEvento = tiposPorEvento.ToDictionary(
        par => par.Key, par => par.Value.ToMessageTypeName(), StringComparer.Ordinal);

    public void MapIncomingToEnvelope(Envelope envelope, Message<string, byte[]> incoming)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(incoming);

        envelope.ContentType = "application/json";
        envelope.Data = incoming.Value;

        if (Guid.TryParse(LerHeader(incoming, "id"), out var eventoId))
        {
            envelope.Id = eventoId;
        }

        if (LerHeader(incoming, "traceparent") is { Length: > 0 } traceparent)
        {
            envelope.ParentId = traceparent;
        }

        var tipo = LerHeader(incoming, "tipo");
        if (string.IsNullOrWhiteSpace(tipo))
        {
            Substituir(envelope, new MensagemInvalida("mensagem sem o header tipo"));
        }
        else if (_tiposPorEvento.TryGetValue(tipo, out var tipoMensagem))
        {
            envelope.MessageType = tipoMensagem;
        }
        else
        {
            Substituir(envelope, new EventoDesconhecido(tipo));
        }
    }

    public void MapEnvelopeToOutgoing(Envelope envelope, Message<string, byte[]> outgoing) =>
        throw new NotSupportedException("O Extrato só consome deste tópico; quem publica é o Debezium.");

    private static void Substituir<T>(Envelope envelope, T mensagem)
    {
        envelope.MessageType = typeof(T).ToMessageTypeName();
        envelope.Data = JsonSerializer.SerializeToUtf8Bytes(mensagem, JsonSerializerOptions.Web);
    }

    private static string? LerHeader(Message<string, byte[]> mensagem, string nome) =>
        mensagem.Headers is not null && mensagem.Headers.TryGetLastBytes(nome, out var bytes) && bytes is not null
            ? Encoding.UTF8.GetString(bytes)
            : null;
}
