using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Sem um mínimo de transferências nos últimos 30 dias, não há média confiável e a regra não dispara.</summary>
public sealed class AvaliadorValorAcimaDaMedia : IAvaliadorRegra
{
    private static readonly TimeSpan Janela = TimeSpan.FromDays(30);

    public CodigoRegra Codigo => CodigoRegra.ValorAcimaDaMedia;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        var multiplicador = regra.Parametro("multiplicador");
        var minimo = regra.Parametro("minimoSaidas");
        var saidas = perfil.SaidasNaJanela(operacao.Momento, Janela).ToList();
        if (saidas.Count < minimo)
        {
            return ResultadoRegra.Nao($"{saidas.Count} transferências em 30 dias: histórico insuficiente para a média");
        }

        var media = saidas.Average(saida => saida.Valor);
        var valor = operacao.Valor.Valor;
        return valor > multiplicador * media
            ? ResultadoRegra.Sim(
                $"Valor {Formatacao.Valor(valor)} acima de {Formatacao.Numero(multiplicador)} vezes a média de 30 dias ({Formatacao.Valor(media)})")
            : ResultadoRegra.Nao($"Valor dentro de {Formatacao.Numero(multiplicador)} vezes a média de 30 dias ({Formatacao.Valor(media)})");
    }
}
