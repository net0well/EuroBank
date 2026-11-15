using EuroBank.Transferencias.Application.Enums;

namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>A situação da key e, quando outro pedido a tem, a impressão digital dele.</summary>
public sealed record ReservaIdempotencia(SituacaoReserva Situacao, string? HashDoPedido = null)
{
    /// <summary>A key é de outro pedido com conteúdo diferente: 422 sem ir ao banco.</summary>
    public bool DeOutroPedido(string hashDoPedido) =>
        Situacao is SituacaoReserva.EmAndamento or SituacaoReserva.Concluida
        && !string.Equals(HashDoPedido, hashDoPedido, StringComparison.Ordinal);
}
