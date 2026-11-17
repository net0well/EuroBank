namespace EuroBank.Antifraude.Features.ValueObjects;

/// <summary>
/// O modelo de dados comum ao treino e à produção: o que se sabe da conta antes da operação. No treino, vem do
/// acumulador que reproduz o histórico gerado; na produção, do perfil no Redis.
/// </summary>
/// <param name="AbertaEm">Nula quando a conta nunca foi vista.</param>
/// <param name="Saldo">O saldo antes da operação; nulo quando desconhecido.</param>
/// <param name="Saidas">Transferências que saíram, em qualquer ordem.</param>
/// <param name="Entradas">Depósitos e transferências recebidas, em qualquer ordem.</param>
/// <param name="Beneficiarios">IBANs (normalizados) para os quais a conta já transferiu.</param>
public sealed record HistoricoConta(
    DateTimeOffset? AbertaEm,
    decimal? Saldo,
    IReadOnlyList<MovimentoHistorico> Saidas,
    IReadOnlyList<MovimentoHistorico> Entradas,
    IReadOnlySet<string> Beneficiarios)
{
    public static HistoricoConta Vazio { get; } = new(null, null, [], [], new HashSet<string>());

    public bool SemHistorico => AbertaEm is null && Saldo is null && Saidas.Count == 0 && Entradas.Count == 0;
}
