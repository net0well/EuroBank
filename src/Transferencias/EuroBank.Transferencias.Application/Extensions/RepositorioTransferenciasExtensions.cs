using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Extensions;

public static class RepositorioTransferenciasExtensions
{
    /// <exception cref="ExcecaoTransferenciaNaoEncontrada">Quando a transferência não existe (vira 404).</exception>
    public static async Task<Transferencia> ObterOuFalharAsync(
        this IRepositorioTransferencias repositorio,
        Guid transferenciaId,
        CancellationToken cancellationToken) =>
        await repositorio.ObterPorIdAsync(new TransferenciaId(transferenciaId), cancellationToken)
        ?? throw new ExcecaoTransferenciaNaoEncontrada(transferenciaId);
}
