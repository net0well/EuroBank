using System.Diagnostics;

namespace EuroBank.Simuladores.Cambio;

public static class Telemetria
{
    public const string NomeFonte = "EuroBank.Simuladores.Cambio";

    public static readonly ActivitySource Fonte = new(NomeFonte);
}
