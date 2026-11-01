using System.Globalization;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.ValueObjects;

/// <summary>
/// Valor monetário em euro, nunca negativo, com no máximo duas casas decimais.
/// </summary>
public sealed record Dinheiro : IComparable<Dinheiro>
{
    public const string Moeda = "EUR";

    private Dinheiro(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public static Dinheiro Zero { get; } = new(0m);

    public bool EhZero => Valor == 0m;

    public static Dinheiro De(decimal valor)
    {
        if (valor < 0m)
        {
            throw new ExcecaoValorInvalido("o valor não pode ser negativo");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new ExcecaoValorInvalido("use no máximo duas casas decimais");
        }

        return new Dinheiro(valor);
    }

    public static Dinheiro operator +(Dinheiro esquerda, Dinheiro direita) => new(esquerda.Valor + direita.Valor);

    public static Dinheiro operator -(Dinheiro esquerda, Dinheiro direita) => De(esquerda.Valor - direita.Valor);

    public static bool operator <(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) < 0;

    public static bool operator >(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) > 0;

    public static bool operator <=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) <= 0;

    public static bool operator >=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) >= 0;

    public int CompareTo(Dinheiro? outro) => outro is null ? 1 : Valor.CompareTo(outro.Valor);

    /// <summary>Formato usado nos eventos e na API: ponto decimal e sempre duas casas ("100.00").</summary>
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);
}
