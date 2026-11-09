namespace EuroBank.Simuladores.Comum;

/// <summary>Uma mensagem lida do Kafka, com os headers que o Outbox Event Router do Debezium envia.</summary>
public sealed record MensagemRecebida(string? Id, string? Tipo, string? Chave, string Payload, string? Traceparent);
