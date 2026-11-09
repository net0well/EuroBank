using System.Diagnostics;

namespace EuroBank.Simuladores.BancoExterno;

public static class Telemetria
{
    public const string NomeFonte = "EuroBank.Simuladores.BancoExterno";

    public static readonly ActivitySource Fonte = new(NomeFonte);
}
