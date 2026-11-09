using System.Text.Json;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Infrastructure.Outbox.TransferObjects;

namespace EuroBank.Transferencias.Infrastructure.Outbox;

/// <summary>
/// Transforma as decisões da saga em linhas da outbox. O tipo_agregado escolhe o tópico
/// (eurobank.${routedByValue}); a chave é a conta nos comandos para Contas (ordem por conta, como o Contas.Worker
/// processa) e a transferência nos demais.
/// </summary>
public sealed class MapeadorMensagensOutbox : IMapeadorMensagensOutbox
{
    public const string ComandosContas = "contas.comandos";
    public const string ComandosBancoExterno = "banco-externo.comandos";
    public const string EventosTransferencias = "transferencias.eventos";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MensagemOutbox Mapear(IEventoDominio evento, string? traceparent) => evento switch
    {
        DebitoSolicitado e => Criar(
            ComandosContas,
            e.ContaOrigemId.ToString(),
            "DebitarTransferenciaExterna",
            new DebitarTransferenciaExternaMensagem(
                e.TransferenciaId.Valor, e.ContaOrigemId, e.Valor.Valor, e.Tarifa.Valor, e.IbanDestino, e.NomeBeneficiario, e.Descricao),
            e,
            traceparent),
        EstornoSolicitado e => Criar(
            ComandosContas,
            e.ContaOrigemId.ToString(),
            "EstornarTransferencia",
            new ComandoContaMensagem(e.TransferenciaId.Valor, e.ContaOrigemId),
            e,
            traceparent),
        LiberacaoTravaSolicitada e => Criar(
            ComandosContas,
            e.ContaOrigemId.ToString(),
            "LiberarTravaTransferencia",
            new ComandoContaMensagem(e.TransferenciaId.Valor, e.ContaOrigemId),
            e,
            traceparent),
        LiquidacaoSolicitada e => Criar(
            ComandosBancoExterno,
            e.TransferenciaId.ToString(),
            "LiquidarTransferencia",
            new LiquidarTransferenciaMensagem(
                e.TransferenciaId.Valor, e.IbanDestino, e.Bic, e.NomeBeneficiario, e.Valor.Valor, Dinheiro.Moeda),
            e,
            traceparent),
        ConsultaLiquidacaoSolicitada e => Criar(
            ComandosBancoExterno,
            e.TransferenciaId.ToString(),
            "ConsultarLiquidacao",
            new ConsultarLiquidacaoMensagem(e.TransferenciaId.Valor),
            e,
            traceparent),
        TransferenciaConcluida e => Criar(
            EventosTransferencias,
            e.TransferenciaId.ToString(),
            nameof(TransferenciaConcluida),
            new TransferenciaConcluidaMensagem(
                e.TransferenciaId.Valor, e.ContaOrigemId, e.Valor.ToString(), e.Tarifa.ToString(), e.OcorridoEm),
            e,
            traceparent),
        TransferenciaCancelada e => Criar(
            EventosTransferencias,
            e.TransferenciaId.ToString(),
            nameof(TransferenciaCancelada),
            new TransferenciaNaoConcluidaMensagem(e.TransferenciaId.Valor, e.ContaOrigemId, e.Motivo.ToString(), e.OcorridoEm),
            e,
            traceparent),
        TransferenciaRecusada e => Criar(
            EventosTransferencias,
            e.TransferenciaId.ToString(),
            nameof(TransferenciaRecusada),
            new TransferenciaNaoConcluidaMensagem(e.TransferenciaId.Valor, e.ContaOrigemId, e.Motivo.ToString(), e.OcorridoEm),
            e,
            traceparent),
        _ => throw new InvalidOperationException($"O evento {evento.GetType().Name} não tem mapeamento para a outbox."),
    };

    private static MensagemOutbox Criar<TPayload>(
        string tipoAgregado,
        string chave,
        string tipo,
        TPayload payload,
        IEventoDominio evento,
        string? traceparent) =>
        new(
            Guid.CreateVersion7(),
            tipoAgregado,
            chave,
            tipo,
            JsonSerializer.Serialize(payload, Json),
            evento.OcorridoEm,
            traceparent);
}
