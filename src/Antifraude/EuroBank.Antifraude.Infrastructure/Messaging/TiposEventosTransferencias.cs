using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Infrastructure.Messaging;

/// <summary>
/// Eventos finais de eurobank.transferencias.eventos: só o cancelamento pelo cliente mexe no antifraude (fecha o caso
/// pendente); os outros são conhecidos e descartados sem alerta.
/// </summary>
public static class TiposEventosTransferencias
{
    public const string Topico = "eurobank.transferencias.eventos";

    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["TransferenciaCancelada"] = typeof(EventoTransferenciaCancelada),
        ["TransferenciaConcluida"] = typeof(EventoTransferenciaSemEfeito),
        ["TransferenciaRecusada"] = typeof(EventoTransferenciaSemEfeito),
        ["TransferenciaDivergente"] = typeof(EventoTransferenciaSemEfeito),
        ["TransferenciaDevolvida"] = typeof(EventoTransferenciaSemEfeito),
    };
}
