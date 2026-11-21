using EuroBank.Shared.ValueObjects;

namespace EuroBank.Transferencias.Application.Commands;

/// <summary>
/// Pedido de câmbio entre contas EuroBank de moedas diferentes (POST /transferencias-cambio). Sem MoedaDoValor, o
/// valor está na moeda da conta de origem.
/// Dispositivo e localização vêm dos headers e não entram na impressão digital da Idempotency-Key.
/// </summary>
public sealed record SolicitarTransferenciaCambio(
    string? IdempotencyKey,
    Guid ContaOrigemId,
    string? IbanDestino,
    decimal Valor,
    string? MoedaDoValor,
    DateTimeOffset? ExecutarEm,
    string? Descricao,
    string? EmailNotificacao,
    string? DispositivoId = null,
    Localizacao? Localizacao = null);
