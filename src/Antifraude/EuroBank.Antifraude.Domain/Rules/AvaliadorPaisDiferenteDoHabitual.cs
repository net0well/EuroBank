using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>
/// O cliente está num país diferente do habitual: o mais frequente nas operações aprovadas dos últimos 30 dias, com um
/// mínimo de operações localizadas para haver hábito.
/// </summary>
public sealed class AvaliadorPaisDiferenteDoHabitual : IAvaliadorRegra
{
    private static readonly TimeSpan Janela = TimeSpan.FromDays(30);

    public CodigoRegra Codigo => CodigoRegra.PaisDiferenteDoHabitual;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (operacao.Localizacao is not { } localizacao)
        {
            return ResultadoRegra.Nao("Operação sem localização informada");
        }

        var minimo = (int)regra.Parametro("minimoOperacoes");
        if (perfil.PaisHabitual(operacao.Momento, Janela, minimo) is not { } habitual)
        {
            return ResultadoRegra.Nao($"Sem país habitual (menos de {minimo} operações localizadas em 30 dias)");
        }

        return localizacao.Pais == habitual
            ? ResultadoRegra.Nao($"Operação no país habitual ({habitual})")
            : ResultadoRegra.Sim($"Operação em {localizacao.Pais}, país habitual {habitual}");
    }
}
