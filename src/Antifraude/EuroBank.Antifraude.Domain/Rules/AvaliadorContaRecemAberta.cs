using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Conta sem data de abertura no perfil é tratada como aberta agora (conta desconhecida é conta nova).</summary>
public sealed class AvaliadorContaRecemAberta : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.ContaRecemAberta;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        var dias = regra.Parametro("dias");
        var minimo = regra.Parametro("valorMinimo");
        var idade = perfil.AbertaEm is { } abertaEm ? operacao.Momento - abertaEm : TimeSpan.Zero;
        var valor = operacao.Valor.Valor;
        var recente = idade < TimeSpan.FromDays((double)dias);
        var idadeEmDias = Formatacao.Numero((decimal)Math.Floor(idade.TotalDays));

        if (recente && valor >= minimo)
        {
            return ResultadoRegra.Sim(
                $"Conta com {idadeEmDias} dias transferindo {Formatacao.Valor(valor)} (a partir de {Formatacao.Valor(minimo)})");
        }

        return ResultadoRegra.Nao(recente
            ? $"Conta com {idadeEmDias} dias, valor abaixo de {Formatacao.Valor(minimo)}"
            : $"Conta com {idadeEmDias} dias");
    }
}
