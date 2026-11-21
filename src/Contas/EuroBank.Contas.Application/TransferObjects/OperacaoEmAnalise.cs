using EuroBank.Contas.Application.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.TransferObjects;

/// <summary>
/// O saque ou a transferência interna como o antifraude a avalia. OperacaoId é novo a cada pedido (Contas não tem
/// Idempotency-Key): repetir o pedido é outra análise.
/// </summary>
public sealed record OperacaoEmAnalise(
    Guid OperacaoId,
    TipoOperacaoRisco Tipo,
    Guid ContaId,
    Dinheiro Valor,
    string? IbanDestino,
    DateTimeOffset Momento,
    string? DispositivoId,
    Localizacao? Localizacao);
