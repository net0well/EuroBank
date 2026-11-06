using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Exceptions;
using EuroBank.Extrato.Domain.Repositories;

namespace EuroBank.Extrato.Application.Extensions;

public static class RepositorioExtratosExtensions
{
    /// <exception cref="ExcecaoExtratoNaoEncontrado">
    /// Evento de uma conta cujo ContaAberta ainda não chegou. Com a ordem por partição isso não deveria acontecer;
    /// se acontecer, a mensagem passa pelo retry e, persistindo, vai para o dead letter.
    /// </exception>
    public static async Task<ExtratoConta> ObterOuFalharAsync(
        this IRepositorioExtratos repositorio,
        Guid contaId,
        CancellationToken cancellationToken) =>
        await repositorio.ObterAsync(contaId, cancellationToken) ?? throw new ExcecaoExtratoNaoEncontrado(contaId);
}
