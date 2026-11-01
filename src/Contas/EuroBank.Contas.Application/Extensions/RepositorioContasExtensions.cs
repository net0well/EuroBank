using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.ValueObjects;

namespace EuroBank.Contas.Application.Extensions;

public static class RepositorioContasExtensions
{
    /// <exception cref="ExcecaoContaNaoEncontrada">Quando a conta não existe (vira 404).</exception>
    public static async Task<Conta> ObterOuFalharAsync(
        this IRepositorioContas repositorio,
        Guid contaId,
        CancellationToken cancellationToken)
    {
        var id = new ContaId(contaId);
        return await repositorio.ObterPorIdAsync(id, cancellationToken) ?? throw new ExcecaoContaNaoEncontrada(id);
    }
}
