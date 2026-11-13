using System.Globalization;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.ValueObjects;

/// <summary>
/// Saldo de uma conta: como o <see cref="Dinheiro"/>, tem moeda e duas casas, mas pode ser negativo (só uma
/// reconciliação deixa a conta devendo). Movimentos são sempre <see cref="Dinheiro"/>, nunca negativos.
/// </summary>
public sealed record Saldo
{
    private Saldo(decimal valor, Moeda moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public decimal Valor { get; }

    public Moeda Moeda { get; }

    public bool EhZero => Valor == 0m;

    public bool EhNegativo => Valor < 0m;

    public static Saldo ZeroEm(Moeda moeda) => new(0m, moeda);

    public static Saldo De(decimal valor, Moeda moeda)
    {
        if (decimal.Round(valor, 2) != valor)
        {
            throw new ExcecaoValorInvalido("use no máximo duas casas decimais");
        }

        return new Saldo(valor, moeda);
    }

    /// <summary>Um valor nunca negativo é sempre um saldo válido.</summary>
    public static implicit operator Saldo(Dinheiro dinheiro)
    {
        ArgumentNullException.ThrowIfNull(dinheiro);
        return new Saldo(dinheiro.Valor, dinheiro.Moeda);
    }

    public static Saldo operator +(Saldo saldo, Dinheiro valor)
    {
        GarantirMesmaMoeda(saldo, valor);
        return new Saldo(saldo.Valor + valor.Valor, saldo.Moeda);
    }

    /// <summary>Pode deixar o saldo negativo: quem decide se pode é o agregado.</summary>
    public static Saldo operator -(Saldo saldo, Dinheiro valor)
    {
        GarantirMesmaMoeda(saldo, valor);
        return new Saldo(saldo.Valor - valor.Valor, saldo.Moeda);
    }

    /// <summary>O saldo dá para pagar este valor?</summary>
    public bool Cobre(Dinheiro valor)
    {
        GarantirMesmaMoeda(this, valor);
        return Valor >= valor.Valor;
    }

    /// <summary>Ponto decimal e sempre duas casas ("100.00", "-52.50"), sem a moeda.</summary>
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    private static void GarantirMesmaMoeda(Saldo saldo, Dinheiro valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (saldo.Moeda != valor.Moeda)
        {
            throw new ExcecaoMoedaDiferente(saldo.Moeda, valor.Moeda);
        }
    }
}
