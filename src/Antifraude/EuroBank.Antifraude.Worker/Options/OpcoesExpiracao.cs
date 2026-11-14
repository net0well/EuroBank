namespace EuroBank.Antifraude.Worker.Options;

/// <summary>Seção "Expiracao" do appsettings.</summary>
public sealed class OpcoesExpiracao
{
    public const string Secao = "Expiracao";

    /// <summary>De quanto em quanto tempo o worker procura casos que passaram do prazo.</summary>
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Quantos casos cada ciclo expira no máximo.</summary>
    public int Lote { get; set; } = 100;
}
