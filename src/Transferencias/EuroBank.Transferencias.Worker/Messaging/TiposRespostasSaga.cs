using EuroBank.Transferencias.Application.Replies;

namespace EuroBank.Transferencias.Worker.Messaging;

/// <summary>Respostas que os participantes mandam ao orquestrador (header "tipo" de cada tópico).</summary>
public static class TiposRespostasSaga
{
    public const string TopicoContas = "eurobank.contas.respostas";

    public const string TopicoBancoExterno = "eurobank.banco-externo.respostas";

    public static IReadOnlyDictionary<string, Type> Contas { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(DebitoRealizado)] = typeof(DebitoRealizado),
        [nameof(DebitoRecusado)] = typeof(DebitoRecusado),
        [nameof(EstornoRealizado)] = typeof(EstornoRealizado),
    };

    public static IReadOnlyDictionary<string, Type> BancoExterno { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(LiquidacaoConfirmada)] = typeof(LiquidacaoConfirmada),
        [nameof(LiquidacaoRecusada)] = typeof(LiquidacaoRecusada),
        [nameof(StatusLiquidacao)] = typeof(StatusLiquidacao),
    };
}
