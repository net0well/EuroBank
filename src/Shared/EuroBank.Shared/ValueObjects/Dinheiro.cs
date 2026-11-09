using System.Globalization;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.ValueObjects;

/// <summary>
/// Valor monetário numa moeda, nunca negativo, com no máximo duas casas decimais. Somar, subtrair ou comparar
/// valores de moedas diferentes é proibido: conversão só pelo câmbio, com cotação explícita.
/// </summary>
public sealed record Dinheiro : IComparable<Dinheiro>
{
    private Dinheiro(decimal valor, Moeda moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public decimal Valor { get; }

    public Moeda Moeda { get; }

    /// <summary>Zero em euro (a moeda padrão das contas).</summary>
    public static Dinheiro Zero { get; } = new(0m, Moeda.EUR);

    public bool EhZero => Valor == 0m;

    public static Dinheiro ZeroEm(Moeda moeda) => new(0m, moeda);

    public static Dinheiro De(decimal valor, Moeda moeda = Moeda.EUR)
    {
        if (valor < 0m)
        {
            throw new ExcecaoValorInvalido("o valor não pode ser negativo");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new ExcecaoValorInvalido("use no máximo duas casas decimais");
        }

        return new Dinheiro(valor, moeda);
    }

    public static Dinheiro operator +(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return new(esquerda.Valor + direita.Valor, esquerda.Moeda);
    }

    public static Dinheiro operator -(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return De(esquerda.Valor - direita.Valor, esquerda.Moeda);
    }

    public static bool operator <(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) < 0;

    public static bool operator >(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) > 0;

    public static bool operator <=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) <= 0;

    public static bool operator >=(Dinheiro esquerda, Dinheiro direita) => esquerda.CompareTo(direita) >= 0;

    public int CompareTo(Dinheiro? outro)
    {
        if (outro is null)
        {
            return 1;
        }

        GarantirMesmaMoeda(this, outro);
        return Valor.CompareTo(outro.Valor);
    }

    /// <summary>Formato usado nos eventos e na API: ponto decimal e sempre duas casas ("100.00"), sem a moeda.</summary>
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    private static void GarantirMesmaMoeda(Dinheiro esquerda, Dinheiro direita)
    {
        if (esquerda.Moeda != direita.Moeda)
        {
            throw new ExcecaoMoedaDiferente(esquerda.Moeda, direita.Moeda);
        }
    }
}
