using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EuroBank.Transferencias.Application.Commands;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// SHA-256 do pedido normalizado. Dois pedidos com a mesma Idempotency-Key só são "o mesmo pedido" se a
/// impressão digital bater; diferenças só de forma (espaços no IBAN, 100 ou 100.00) não contam.
/// </summary>
public static class ImpressaoDigitalPedido
{
    public static string Calcular(SolicitarTransferenciaExterna pedido)
    {
        var normalizado = new
        {
            pedido.ContaOrigemId,
            IbanDestino = (pedido.IbanDestino ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant(),
            Bic = (pedido.Bic ?? string.Empty).Trim().ToUpperInvariant(),
            NomeBeneficiario = (pedido.NomeBeneficiario ?? string.Empty).Trim(),
            Valor = pedido.Valor.ToString("0.00########", CultureInfo.InvariantCulture),
            ExecutarEm = pedido.ExecutarEm?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            pedido.Descricao,
            EmailNotificacao = pedido.EmailNotificacao?.Trim(),
        };

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizado)));
        return Convert.ToHexStringLower(bytes);
    }
}
