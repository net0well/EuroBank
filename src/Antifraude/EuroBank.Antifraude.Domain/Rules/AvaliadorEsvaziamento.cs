using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Sem saldo conhecido (ou com saldo zerado ou negativo), a regra não dispara.</summary>
public sealed class AvaliadorEsvaziamento : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.Esvaziamento;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (perfil.Saldo is not > 0m)
        {
            return ResultadoRegra.Nao("Saldo da conta desconhecido ou sem saldo positivo");
        }

        var percentual = regra.Parametro("percentualSaldo");
        var saldo = perfil.Saldo.Value;
        var valor = operacao.Valor.Valor;
        return valor >= percentual * saldo
            ? ResultadoRegra.Sim(
                $"Valor {Formatacao.Valor(valor)} é {Formatacao.Numero(percentual * 100)}% ou mais do saldo ({Formatacao.Valor(saldo)})")
            : ResultadoRegra.Nao($"Valor abaixo de {Formatacao.Numero(percentual * 100)}% do saldo ({Formatacao.Valor(saldo)})");
    }
}
