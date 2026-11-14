using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Messaging;

/// <summary>
/// Eventos finais que as notificações entendem em eurobank.transferencias.eventos (header "tipo") e o DTO de cada
/// um. Tipos fora da tabela viram EventoDesconhecido e são ignorados com alerta.
/// </summary>
public static class TiposEventosTransferencias
{
    public const string Topico = "eurobank.transferencias.eventos";

    public const string Concluida = "TransferenciaConcluida";
    public const string Cancelada = "TransferenciaCancelada";
    public const string Recusada = "TransferenciaRecusada";
    public const string Divergente = "TransferenciaDivergente";
    public const string Devolvida = "TransferenciaDevolvida";

    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [Concluida] = typeof(EventoTransferenciaConcluida),
        [Cancelada] = typeof(EventoTransferenciaCancelada),
        [Recusada] = typeof(EventoTransferenciaRecusada),
        [Divergente] = typeof(EventoTransferenciaDivergente),
        [Devolvida] = typeof(EventoTransferenciaDevolvida),
    };
}
