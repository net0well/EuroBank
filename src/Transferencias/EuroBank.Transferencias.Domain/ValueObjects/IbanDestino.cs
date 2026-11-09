using System.Text.RegularExpressions;
using EuroBank.Transferencias.Domain.Exceptions;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>
/// IBAN de destino informado pelo cliente. Só o formato é conferido, sem o dígito verificador: assim os finais
/// que comandam o banco externo simulado (99 a 94) podem ser usados em qualquer IBAN.
/// </summary>
public sealed partial record IbanDestino
{
    private const string PaisEuroBank = "PT";
    private const string CodigoBancoEuroBank = "EURO";

    private IbanDestino(string valor) => Valor = valor;

    public string Valor { get; }

    /// <summary>IBANs do EuroBank: país PT e código de banco EURO logo depois dos dígitos verificadores.</summary>
    public bool EhEuroBank =>
        Valor.StartsWith(PaisEuroBank, StringComparison.Ordinal)
        && Valor.AsSpan(4, CodigoBancoEuroBank.Length).SequenceEqual(CodigoBancoEuroBank);

    public static IbanDestino Criar(string? valor)
    {
        var normalizado = (valor ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (!Formato().IsMatch(normalizado))
        {
            throw new ExcecaoIbanDestinoInvalido(valor ?? string.Empty);
        }

        return new IbanDestino(normalizado);
    }

    public override string ToString() => Valor;

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")]
    private static partial Regex Formato();
}
