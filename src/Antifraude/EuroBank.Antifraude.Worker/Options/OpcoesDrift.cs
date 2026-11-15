namespace EuroBank.Antifraude.Worker.Options;

/// <summary>Seção "Drift" do appsettings: de quanto em quanto tempo medir, sobre qual janela e com quais limites.</summary>
public sealed class OpcoesDrift
{
    public const string Secao = "Drift";

    public TimeSpan Intervalo { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>As análises criadas nesta janela, até agora, contra o treino da versão ativa.</summary>
    public TimeSpan Janela { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Abaixo disso a medição fica como amostra insuficiente, sem PSI.</summary>
    public int AmostraMinima { get; set; } = 100;

    /// <summary>Teto de análises por medição (as mais recentes), para a memória do worker não crescer com a carga.</summary>
    public int AmostraMaxima { get; set; } = 20_000;

    public double LimiarModerado { get; set; } = 0.1;

    public double LimiarSignificativo { get; set; } = 0.25;

    /// <summary>Medições mais antigas que isto são apagadas.</summary>
    public TimeSpan Retencao { get; set; } = TimeSpan.FromDays(7);
}
