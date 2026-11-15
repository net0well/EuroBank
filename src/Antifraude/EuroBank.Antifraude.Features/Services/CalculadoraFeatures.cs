using EuroBank.Antifraude.Features.Policies;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Features.Services;

/// <summary>
/// Calcula as features de uma operação a partir do histórico da conta. É o único lugar com essas contas: o treino e a
/// produção chamam o mesmo método, e o que muda entre eles é só de onde vem o histórico.
/// </summary>
public static class CalculadoraFeatures
{
    public static VetorFeatures Calcular(HistoricoConta historico, OperacaoAvaliada operacao)
    {
        ArgumentNullException.ThrowIfNull(historico);
        ArgumentNullException.ThrowIfNull(operacao);
        var momento = operacao.Momento;
        var valor = operacao.Valor;

        var saidasDoMes = Janela(historico.Saidas, momento, JanelasFeatures.Media).ToList();
        var media = saidasDoMes.Count == 0 ? 0m : saidasDoMes.Average(saida => saida.Valor);
        var valorSobreMedia = media <= 0 ? 0 : Math.Min((double)(valor / media), JanelasFeatures.TetoValorSobreMedia);

        var entradasDoDia = Janela(historico.Entradas, momento, JanelasFeatures.Entradas).Sum(entrada => entrada.Valor);

        return new VetorFeatures(
            ValorSobreMedia: (float)valorSobreMedia,
            SaidasUltimaHora: Janela(historico.Saidas, momento, JanelasFeatures.Velocidade).Count(),
            PequenasEm15Min: Janela(historico.Saidas, momento, JanelasFeatures.ValoresPequenos).Count(saida => saida.Valor < JanelasFeatures.ValorPequeno),
            BeneficiarioNovo: operacao.IbanDestino is { } iban && !historico.Beneficiarios.Contains(iban) ? 1 : 0,
            IdadeContaDias: historico.AbertaEm is { } abertaEm ? (float)Math.Max(0, (momento - abertaEm).TotalDays) : 0,
            HoraDoDia: momento.UtcDateTime.Hour,
            FracaoDoSaldo: (float)FracaoDoSaldo(historico.Saldo, valor),
            LogValor: (float)Math.Log10(1 + (double)Math.Max(0m, valor)),
            PaisDeRisco: operacao.PaisDeRisco ? 1 : 0,
            SemHistorico: historico.SemHistorico ? 1 : 0,
            EntradasSobreValor: valor <= 0 ? 0 : (float)Math.Min((double)(entradasDoDia / valor), JanelasFeatures.TetoRazao));
    }

    /// <summary>Movimentos antes do momento, dentro da janela que termina nele.</summary>
    private static IEnumerable<MovimentoHistorico> Janela(IEnumerable<MovimentoHistorico> movimentos, DateTimeOffset momento, TimeSpan janela) =>
        movimentos.Where(movimento => movimento.Momento < momento && movimento.Momento >= momento - janela);

    /// <summary>Saldo desconhecido: 0 (sem sinal). Saldo zero ou negativo com valor a sair: o teto.</summary>
    private static double FracaoDoSaldo(decimal? saldo, decimal valor) => saldo switch
    {
        null => 0,
        > 0 => Math.Min((double)(valor / saldo.Value), JanelasFeatures.TetoRazao),
        _ => valor > 0 ? JanelasFeatures.TetoRazao : 0,
    };
}
