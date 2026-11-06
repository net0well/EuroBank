using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EuroBank.Extrato.Application.TransferObjects;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Util;

namespace EuroBank.Extrato.Infrastructure.Messaging;

/// <summary>
/// Traduz as mensagens que o Outbox Event Router do Debezium publica em envelopes do Wolverine:
/// <list type="bullet">
/// <item>header <c>tipo</c>: escolhe o tipo .NET da mensagem e, com ele, o manipulador;</item>
/// <item>header <c>id</c> (id do evento na outbox): vira o id do envelope, chave de deduplicação da inbox durável;</item>
/// <item>header <c>traceparent</c>: vira o pai da Activity, continuando o trace iniciado na API de Contas;</item>
/// <item>corpo: o JSON do evento, desserializado no DTO do Extrato.</item>
/// </list>
/// </summary>
public sealed class MapeadorEnvelopeDebezium : IKafkaEnvelopeMapper
{
    private static readonly Dictionary<string, string> TiposPorEvento = new(StringComparer.Ordinal)
    {
        ["ContaAberta"] = typeof(EventoContaAberta).ToMessageTypeName(),
        ["DepositoRealizado"] = typeof(EventoDepositoRealizado).ToMessageTypeName(),
        ["SaqueRealizado"] = typeof(EventoSaqueRealizado).ToMessageTypeName(),
        ["TransferenciaEnviada"] = typeof(EventoTransferenciaEnviada).ToMessageTypeName(),
        ["TransferenciaRecebida"] = typeof(EventoTransferenciaRecebida).ToMessageTypeName(),
        ["ContaBloqueada"] = typeof(EventoContaBloqueada).ToMessageTypeName(),
        ["ContaDesbloqueada"] = typeof(EventoContaDesbloqueada).ToMessageTypeName(),
        ["ContaEncerrada"] = typeof(EventoContaEncerrada).ToMessageTypeName(),
    };

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
        else if (TiposPorEvento.TryGetValue(tipo, out var tipoMensagem))
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
