using System.Text.Json;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Infrastructure.Outbox.TransferObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;

namespace EuroBank.Contas.Infrastructure.Outbox;

/// <summary>
/// Converte eventos de domínio em linhas da outbox. O mapeamento é explícito: o payload publicado é um contrato
/// de integração, independente da forma interna dos eventos de domínio.
/// </summary>
public sealed class MapeadorMensagensOutbox : IMapeadorMensagensOutbox
{
    /// <summary>Valor de tipo_agregado; o Debezium roteia para o tópico eurobank.contas.eventos.</summary>
    public const string TipoAgregado = "contas";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MensagemOutbox Mapear(IEventoDominio evento, string? traceparent) => evento switch
    {
        ContaAberta e => Criar(e, traceparent, new ContaAbertaMensagem(
            e.ContaId.Valor, e.Iban.Valor, e.Titular.Nome, e.Versao, e.OcorridoEm)),
        DepositoRealizado e => Criar(e, traceparent, new MovimentacaoMensagem(
            e.ContaId.Valor, e.Valor.ToString(), e.SaldoApos.ToString(), e.Versao, e.OcorridoEm)),
        SaqueRealizado e => Criar(e, traceparent, new MovimentacaoMensagem(
            e.ContaId.Valor, e.Valor.ToString(), e.SaldoApos.ToString(), e.Versao, e.OcorridoEm)),
        TransferenciaEnviada e => Criar(e, traceparent, new TransferenciaEnviadaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanDestino.Valor,
            e.Valor.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        TransferenciaRecebida e => Criar(e, traceparent, new TransferenciaRecebidaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanOrigem.Valor,
            e.Valor.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        ContaBloqueada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        ContaDesbloqueada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        ContaEncerrada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        _ => throw new InvalidOperationException($"O evento {evento.GetType().Name} não tem mapeamento para a outbox."),
    };

    private static MensagemOutbox Criar<TPayload>(IEventoDeConta evento, string? traceparent, TPayload payload) =>
        new(
            Guid.CreateVersion7(),
            TipoAgregado,
            evento.ContaId.ToString(),
            evento.GetType().Name,
            JsonSerializer.Serialize(payload, Json),
            evento.OcorridoEm,
            traceparent);
}
