using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Queries;

public sealed class ContasConhecidas(TransferenciasDbContext contexto) : IContasConhecidas
{
    public async Task<ContaEuroBank?> ObterPorIdAsync(Guid contaId, CancellationToken cancellationToken) =>
        ParaConta(await contexto.ContasConhecidas.AsNoTracking().FirstOrDefaultAsync(conta => conta.Id == contaId, cancellationToken));

    public async Task<ContaEuroBank?> ObterPorIbanAsync(string iban, CancellationToken cancellationToken) =>
        ParaConta(await contexto.ContasConhecidas.AsNoTracking().FirstOrDefaultAsync(conta => conta.Iban == iban, cancellationToken));

    /// <summary>INSERT com ON CONFLICT DO NOTHING: o mesmo ContaAberta entregue duas vezes não muda nada.</summary>
    public async Task<bool> RegistrarAsync(ContaEuroBank conta, DateTimeOffset abertaEm, CancellationToken cancellationToken) =>
        await contexto.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO contas_conhecidas (id, iban, titular, moeda, aberta_em)
            VALUES ({conta.Id}, {conta.Iban}, {conta.Titular}, {conta.Moeda.ToString()}, {abertaEm})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken) == 1;

    private static ContaEuroBank? ParaConta(ContaConhecida? conta) =>
        conta is null ? null : new ContaEuroBank(conta.Id, conta.Iban, conta.Titular, conta.Moeda);
}
