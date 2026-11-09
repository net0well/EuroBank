namespace EuroBank.Transferencias.Worker.Options;

/// <summary>Seção "Varredura" do appsettings.</summary>
public sealed class OpcoesVarredura
{
    public const string Secao = "Varredura";

    /// <summary>De quanto em quanto tempo a varredura procura agendadas e prazos vencidos.</summary>
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Quantas transferências cada ciclo avança no máximo.</summary>
    public int Lote { get; set; } = 50;
}
