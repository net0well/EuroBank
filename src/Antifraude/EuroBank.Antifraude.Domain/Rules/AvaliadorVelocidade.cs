using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Conta as transferências da janela mais a atual.</summary>
public sealed class AvaliadorVelocidade : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.Velocidade;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        var quantidade = regra.Parametro("quantidade");
        var minutos = regra.Parametro("janelaMinutos");
        var total = perfil.SaidasNaJanela(operacao.Momento, TimeSpan.FromMinutes((double)minutos)).Count() + 1;
        var descricao = $"{total} transferências em {Formatacao.Numero(minutos)} min, contando a atual";
        return total >= quantidade
            ? ResultadoRegra.Sim($"{descricao} (limite {Formatacao.Numero(quantidade)})")
            : ResultadoRegra.Nao(descricao);
    }
}
