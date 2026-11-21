using System.Text.Json;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Infrastructure.Outbox.TransferObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;

namespace EuroBank.Antifraude.Infrastructure.Outbox;

/// <summary>
/// Eventos do Antifraude na outbox: todos vão para eurobank.antifraude.eventos com o id da operação como chave, para
/// os eventos de uma mesma transferência chegarem em ordem (a saga consome por transferência).
/// </summary>
public sealed class MapeadorMensagensOutbox : IMapeadorMensagensOutbox
{
    public const string EventosAntifraude = "antifraude.eventos";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MensagemOutbox Mapear(IEventoDominio evento, string? traceparent) => evento switch
    {
        AnaliseDecidida e => Criar(
            e.OperacaoId,
            nameof(AnaliseDecidida),
            new AnaliseDecididaMensagem(
                e.AnaliseId.Valor,
                e.OperacaoId,
                e.ContaId,
                e.Decisao.ToString(),
                e.Origem.ToString(),
                e.ScoreFinal,
                e.Analista,
                e.Justificativa,
                e.OcorridoEm,
                e.MomentoOperacao,
                e.DispositivoId,
                e.Localizacao),
            e,
            traceparent),
        CasoAberto e => Criar(
            e.OperacaoId,
            nameof(CasoAberto),
            new CasoAbertoMensagem(e.CasoId.Valor, e.AnaliseId.Valor, e.OperacaoId, e.ContaId, e.ScoreFinal, e.ExpiraEm, e.OcorridoEm),
            e,
            traceparent),
        _ => throw new InvalidOperationException($"O evento {evento.GetType().Name} não tem mapeamento para a outbox."),
    };

    private static MensagemOutbox Criar<TPayload>(Guid operacaoId, string tipo, TPayload payload, IEventoDominio evento, string? traceparent) =>
        new(
            Guid.CreateVersion7(),
            EventosAntifraude,
            operacaoId.ToString(),
            tipo,
            JsonSerializer.Serialize(payload, Json),
            evento.OcorridoEm,
            traceparent);
}
