using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Só com histórico suficiente nos últimos 30 dias dá para dizer que uma hora (UTC) é incomum.</summary>
public sealed class AvaliadorHorarioIncomum : IAvaliadorRegra
{
    private static readonly TimeSpan Janela = TimeSpan.FromDays(30);

    public CodigoRegra Codigo => CodigoRegra.HorarioIncomum;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        var minimo = regra.Parametro("minimoSaidas");
        var saidas = perfil.SaidasNaJanela(operacao.Momento, Janela).ToList();
        if (saidas.Count < minimo)
        {
            return ResultadoRegra.Nao($"{saidas.Count} transferências em 30 dias: histórico insuficiente para o horário");
        }

        var hora = operacao.Momento.UtcDateTime.Hour;
        return saidas.Any(saida => saida.Momento.UtcDateTime.Hour == hora)
            ? ResultadoRegra.Nao($"A conta já transferiu às {hora:00}h UTC")
            : ResultadoRegra.Sim($"Nenhuma transferência às {hora:00}h UTC nos últimos 30 dias");
    }
}
