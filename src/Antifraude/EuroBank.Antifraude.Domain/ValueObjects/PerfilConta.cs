using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O comportamento da conta até agora, montado a partir dos eventos de Contas e de Transferencias (no Redis). É
/// derivado e reconstruível: sem perfil, a conta é avaliada como nova (sem idade, sem saldo, sem histórico).
/// </summary>
/// <param name="Saidas">Transferências dos últimos 30 dias (pelo menos), em qualquer ordem.</param>
/// <param name="BeneficiariosConhecidos">IBANs para os quais a conta já transferiu, normalizados.</param>
/// <param name="Entradas">Depósitos e transferências recebidas dos últimos 30 dias (pelo menos).</param>
/// <param name="DispositivosConhecidos">Dispositivos de operações aprovadas da conta.</param>
/// <param name="Localizacoes">Onde estavam as operações aprovadas dos últimos 30 dias (pelo menos).</param>
public sealed record PerfilConta(
    Guid ContaId,
    DateTimeOffset? AbertaEm,
    decimal? Saldo,
    IReadOnlyList<SaidaConta> Saidas,
    IReadOnlySet<string> BeneficiariosConhecidos,
    IReadOnlyList<EntradaConta>? Entradas = null,
    IReadOnlySet<string>? DispositivosConhecidos = null,
    IReadOnlyList<LocalizacaoConta>? Localizacoes = null)
{
    public IReadOnlyList<EntradaConta> Entradas { get; } = Entradas ?? [];

    public IReadOnlySet<string> DispositivosConhecidos { get; } = DispositivosConhecidos ?? new HashSet<string>();

    public IReadOnlyList<LocalizacaoConta> Localizacoes { get; } = Localizacoes ?? [];

    public static PerfilConta Vazio(Guid contaId) => new(contaId, null, null, [], new HashSet<string>());

    /// <summary>Saídas anteriores ao momento, dentro da janela que termina nele.</summary>
    public IEnumerable<SaidaConta> SaidasNaJanela(DateTimeOffset momento, TimeSpan janela) =>
        Saidas.Where(saida => saida.Momento < momento && saida.Momento >= momento - janela);

    /// <summary>A localização da operação aprovada mais recente antes do momento; nula sem nenhuma.</summary>
    public LocalizacaoConta? UltimaLocalizacao(DateTimeOffset momento) =>
        Localizacoes.Where(localizacao => localizacao.Momento < momento).MaxBy(localizacao => localizacao.Momento);

    /// <summary>
    /// O país mais frequente nas operações aprovadas da janela; nulo com menos operações localizadas que o mínimo (não
    /// há hábito para comparar). Empate: o mais recente.
    /// </summary>
    public string? PaisHabitual(DateTimeOffset momento, TimeSpan janela, int minimoOperacoes)
    {
        var naJanela = Localizacoes.Where(localizacao => localizacao.Momento < momento && localizacao.Momento >= momento - janela).ToList();
        return naJanela.Count < minimoOperacoes
            ? null
            : naJanela.GroupBy(localizacao => localizacao.Localizacao.Pais)
                .OrderByDescending(grupo => grupo.Count())
                .ThenByDescending(grupo => grupo.Max(localizacao => localizacao.Momento))
                .First()
                .Key;
    }

    /// <summary>O perfil no modelo de dados das features, o mesmo que o treino monta a partir do histórico gerado.</summary>
    public HistoricoConta ParaHistorico() => new(
        AbertaEm,
        Saldo,
        Saidas.Select(saida => new MovimentoHistorico(saida.Momento, saida.Valor, saida.IbanDestino)).ToList(),
        Entradas.Select(entrada => new MovimentoHistorico(entrada.Momento, entrada.Valor)).ToList(),
        BeneficiariosConhecidos);
}
