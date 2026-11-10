using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EuroBank.Transferencias.Application.Commands;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// SHA-256 do pedido normalizado. Dois pedidos com a mesma Idempotency-Key só são "o mesmo pedido" se a
/// impressão digital bater; diferenças só de forma (espaços no IBAN, 100 ou 100.00, "usd" ou "USD") não contam.
/// </summary>
public static class ImpressaoDigitalPedido
{
    public static string Calcular(SolicitarTransferenciaExterna pedido) => Hash(new
    {
        Tipo = "Externa",
        pedido.ContaOrigemId,
        IbanDestino = Iban(pedido.IbanDestino),
        Bic = (pedido.Bic ?? string.Empty).Trim().ToUpperInvariant(),
        NomeBeneficiario = (pedido.NomeBeneficiario ?? string.Empty).Trim(),
        Valor = Valor(pedido.Valor),
        MoedaDoValor = Moeda(pedido.MoedaDoValor),
        MoedaDestino = Moeda(pedido.MoedaDestino),
        ExecutarEm = Data(pedido.ExecutarEm),
        pedido.Descricao,
        EmailNotificacao = pedido.EmailNotificacao?.Trim(),
    });

    public static string Calcular(SolicitarTransferenciaCambio pedido) => Hash(new
    {
        Tipo = "CambioInterno",
        pedido.ContaOrigemId,
        IbanDestino = Iban(pedido.IbanDestino),
        Valor = Valor(pedido.Valor),
        MoedaDoValor = Moeda(pedido.MoedaDoValor),
        ExecutarEm = Data(pedido.ExecutarEm),
        pedido.Descricao,
        EmailNotificacao = pedido.EmailNotificacao?.Trim(),
    });

    private static string Iban(string? iban) =>
        (iban ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static string Valor(decimal valor) => valor.ToString("0.00########", CultureInfo.InvariantCulture);

    private static string? Moeda(string? moeda) => moeda?.Trim().ToUpperInvariant();

    private static string? Data(DateTimeOffset? data) => data?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string Hash<T>(T normalizado) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizado))));
}
