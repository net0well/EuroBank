using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.TransferObjects;

namespace EuroBank.Transferencias.Worker.Messaging;

/// <summary>Mensagens que o orquestrador consome (header "tipo" de cada tópico).</summary>
public static class TiposRespostasSaga
{
    public const string TopicoContas = "eurobank.contas.respostas";

    public const string TopicoBancoExterno = "eurobank.banco-externo.respostas";

    public const string TopicoCambio = "eurobank.cambio.respostas";

    /// <summary>Eventos de Contas: só o ContaAberta interessa (alimenta a projeção local de contas).</summary>
    public const string TopicoEventosContas = "eurobank.contas.eventos";

    public static IReadOnlyDictionary<string, Type> Contas { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(DebitoRealizado)] = typeof(DebitoRealizado),
        [nameof(DebitoRecusado)] = typeof(DebitoRecusado),
        [nameof(EstornoRealizado)] = typeof(EstornoRealizado),
        [nameof(CreditoRealizado)] = typeof(CreditoRealizado),
        [nameof(CreditoRecusado)] = typeof(CreditoRecusado),
    };

    public static IReadOnlyDictionary<string, Type> BancoExterno { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(LiquidacaoConfirmada)] = typeof(LiquidacaoConfirmada),
        [nameof(LiquidacaoRecusada)] = typeof(LiquidacaoRecusada),
        [nameof(StatusLiquidacao)] = typeof(StatusLiquidacao),
    };

    public static IReadOnlyDictionary<string, Type> Cambio { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(CotacaoEmitida)] = typeof(CotacaoEmitida),
        [nameof(CotacaoIndisponivel)] = typeof(CotacaoIndisponivel),
    };

    public static IReadOnlyDictionary<string, Type> EventosContas { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["ContaAberta"] = typeof(EventoContaAberta),
    };
}
