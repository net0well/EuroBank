using System.Diagnostics;
using System.Text.Json;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Infrastructure.Outbox.TransferObjects;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Infrastructure.Outbox;

/// <summary>
/// Respostas ao orquestrador enfileiradas na outbox. O Debezium as publica em eurobank.contas.respostas com a
/// chave = id da transferência, na mesma transação da alteração (ou da recusa) na conta.
/// </summary>
public sealed class RespostasSagaOutbox(IFilaOutbox fila, TimeProvider relogio) : IRespostasSaga
{
    /// <summary>Valor de tipo_agregado das respostas; o Debezium roteia para eurobank.contas.respostas.</summary>
    public const string TipoAgregado = "contas.respostas";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void DebitoRealizado(Guid transferenciaId, Guid contaId, Dinheiro valor, Dinheiro tarifa) =>
        Enfileirar(transferenciaId, "DebitoRealizado",
            new DebitoRealizadoMensagem(transferenciaId, contaId, valor.ToString(), tarifa.ToString()));

    public void DebitoRecusado(Guid transferenciaId, Guid contaId, string motivo) =>
        Enfileirar(transferenciaId, "DebitoRecusado", new DebitoRecusadoMensagem(transferenciaId, contaId, motivo));

    public void CreditoRealizado(Guid transferenciaId, Guid contaId, Dinheiro valor) =>
        Enfileirar(transferenciaId, "CreditoRealizado",
            new CreditoRealizadoMensagem(transferenciaId, contaId, valor.ToString(), valor.Moeda.ToString()));

    public void CreditoRecusado(Guid transferenciaId, Guid contaId, string motivo) =>
        Enfileirar(transferenciaId, "CreditoRecusado", new CreditoRecusadoMensagem(transferenciaId, contaId, motivo));

    public void ReconciliacaoRealizada(Guid transferenciaId, Guid contaId, Dinheiro valorDebitado) =>
        Enfileirar(transferenciaId, "ReconciliacaoRealizada",
            new ValorDaSagaMensagem(transferenciaId, contaId, valorDebitado.ToString(), valorDebitado.Moeda.ToString()));

    public void ReconciliacaoRecusada(Guid transferenciaId, Guid contaId, string motivo) =>
        Enfileirar(transferenciaId, "ReconciliacaoRecusada", new CreditoRecusadoMensagem(transferenciaId, contaId, motivo));

    public void DevolucaoCreditada(Guid transferenciaId, Guid contaId, Dinheiro valor) =>
        Enfileirar(transferenciaId, "DevolucaoCreditada",
            new ValorDaSagaMensagem(transferenciaId, contaId, valor.ToString(), valor.Moeda.ToString()));

    public void DevolucaoRecusada(Guid transferenciaId, Guid contaId, string motivo) =>
        Enfileirar(transferenciaId, "DevolucaoRecusada", new CreditoRecusadoMensagem(transferenciaId, contaId, motivo));

    public void EstornoRealizado(Guid transferenciaId, Guid contaId, Dinheiro valorDevolvido) =>
        Enfileirar(transferenciaId, "EstornoRealizado",
            new EstornoRealizadoMensagem(transferenciaId, contaId, valorDevolvido.ToString()));

    private void Enfileirar<TPayload>(Guid transferenciaId, string tipo, TPayload payload) =>
        fila.Enfileirar(new MensagemOutbox(
            Guid.CreateVersion7(),
            TipoAgregado,
            transferenciaId.ToString(),
            tipo,
            JsonSerializer.Serialize(payload, Json),
            relogio.GetUtcNow(),
            Activity.Current?.Id));
}
