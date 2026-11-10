namespace EuroBank.Simuladores.Cambio.Regras;

/// <summary>Seção "Cambio" do appsettings.</summary>
public sealed class OpcoesCambio
{
    public const string Secao = "Cambio";

    /// <summary>Tempo até responder uma cotação.</summary>
    public int LatenciaMs { get; set; } = 500;

    /// <summary>Por quanto tempo a taxa cotada fica travada.</summary>
    public int ValidadeMinutos { get; set; } = 10;

    /// <summary>Margem do banco sobre a taxa média (0,01 = 1%), sempre contra o cliente.</summary>
    public decimal Spread { get; set; } = 0.01m;
}
