namespace EuroBank.Antifraude.Application.Options;

/// <summary>Seção "Revisao".</summary>
public sealed class OpcoesRevisao
{
    public const string Secao = "Revisao";

    /// <summary>Quanto tempo o caso espera um analista antes de expirar (30 min no laboratório).</summary>
    public TimeSpan PrazoCaso { get; set; } = TimeSpan.FromMinutes(30);
}
