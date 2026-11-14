using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>
/// Valores pequenos seguidos costumam testar se a conta funciona antes do golpe: conta os pequenos da janela mais a
/// atual, se ela também for pequena. A transferência grande logo depois dos testes também dispara.
/// </summary>
public sealed class AvaliadorTesteDeCartao : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.TesteDeCartao;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        var quantidade = regra.Parametro("quantidade");
        var maximo = regra.Parametro("valorMaximo");
        var minutos = regra.Parametro("janelaMinutos");
        var pequenas = perfil.SaidasNaJanela(operacao.Momento, TimeSpan.FromMinutes((double)minutos))
            .Count(saida => saida.Valor < maximo);
        if (operacao.Valor.Valor < maximo)
        {
            pequenas++;
        }

        var descricao = $"{pequenas} transferências abaixo de {Formatacao.Valor(maximo)} em {Formatacao.Numero(minutos)} min";
        return pequenas >= quantidade
            ? ResultadoRegra.Sim($"{descricao} (limite {Formatacao.Numero(quantidade)})")
            : ResultadoRegra.Nao(descricao);
    }
}
