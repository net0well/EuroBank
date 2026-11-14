namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O comportamento da conta até agora, montado a partir dos eventos de Contas e de Transferencias (no Redis). É
/// derivado e reconstruível: sem perfil, a conta é avaliada como nova (sem idade, sem saldo, sem histórico).
/// </summary>
/// <param name="Saidas">Transferências dos últimos 30 dias (pelo menos), em qualquer ordem.</param>
/// <param name="BeneficiariosConhecidos">IBANs para os quais a conta já transferiu, normalizados.</param>
public sealed record PerfilConta(
    Guid ContaId,
    DateTimeOffset? AbertaEm,
    decimal? Saldo,
    IReadOnlyList<SaidaConta> Saidas,
    IReadOnlySet<string> BeneficiariosConhecidos)
{
    public static PerfilConta Vazio(Guid contaId) => new(contaId, null, null, [], new HashSet<string>());

    /// <summary>Saídas anteriores ao momento, dentro da janela que termina nele.</summary>
    public IEnumerable<SaidaConta> SaidasNaJanela(DateTimeOffset momento, TimeSpan janela) =>
        Saidas.Where(saida => saida.Momento < momento && saida.Momento >= momento - janela);
}
