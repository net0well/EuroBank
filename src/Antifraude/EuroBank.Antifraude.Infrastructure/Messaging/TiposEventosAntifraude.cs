using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Infrastructure.Messaging;

/// <summary>
/// O próprio tópico do antifraude: AnaliseDecidida ensina ao perfil o dispositivo e a localização das aprovadas;
/// CasoAberto é conhecido e descartado.
/// </summary>
public static class TiposEventosAntifraude
{
    public const string Topico = "eurobank.antifraude.eventos";

    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["AnaliseDecidida"] = typeof(EventoAnaliseDecidida),
        ["CasoAberto"] = typeof(EventoAntifraudeSemEfeito),
    };
}
