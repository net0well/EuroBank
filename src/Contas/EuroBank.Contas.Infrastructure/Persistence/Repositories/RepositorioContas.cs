using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Infrastructure.Persistence.Repositories;

public sealed class RepositorioContas(ContasDbContext contexto) : IRepositorioContas
{
    public Task<Conta?> ObterPorIdAsync(ContaId id, CancellationToken cancellationToken) =>
        contexto.Contas.FirstOrDefaultAsync(conta => conta.Id == id, cancellationToken);

    public Task<Conta?> ObterPorIbanAsync(Iban iban, CancellationToken cancellationToken) =>
        contexto.Contas.FirstOrDefaultAsync(conta => conta.Iban == iban, cancellationToken);

    public Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken) =>
        contexto.Contas.AnyAsync(cancellationToken);

    public void Adicionar(Conta conta) => contexto.Contas.Add(conta);
}
