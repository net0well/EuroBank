namespace EuroBank.Antifraude.Features.Policies;

/// <summary>
/// As janelas e os limites das features, os mesmos no treino e na produção. A retenção do histórico também vale para
/// o perfil no Redis: guardar menos ali mudaria as features em relação ao treino.
/// </summary>
public static class JanelasFeatures
{
    /// <summary>Quanto do histórico de saídas e entradas é guardado: um dia a mais que a maior janela (30 dias).</summary>
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(31);

    public static readonly TimeSpan Media = TimeSpan.FromDays(30);

    public static readonly TimeSpan Velocidade = TimeSpan.FromHours(1);

    public static readonly TimeSpan ValoresPequenos = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan Entradas = TimeSpan.FromHours(24);

    /// <summary>Abaixo disto, na moeda da conta, a saída conta como valor pequeno (teste de cartão).</summary>
    public const decimal ValorPequeno = 10m;

    /// <summary>Teto das razões, para um valor fora do comum não distorcer o modelo.</summary>
    public const double TetoValorSobreMedia = 100;

    public const double TetoRazao = 10;
}
