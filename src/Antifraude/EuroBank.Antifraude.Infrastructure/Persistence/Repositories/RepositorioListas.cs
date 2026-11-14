using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioListas(AntifraudeDbContext contexto) : IRepositorioListas
{
    public async Task<ListasRisco> ObterAsync(CancellationToken cancellationToken)
    {
        var ibans = await contexto.IbansBloqueados.AsNoTracking().Select(bloqueado => bloqueado.Iban).ToListAsync(cancellationToken);
        var paises = await contexto.PaisesRisco.AsNoTracking().Select(pais => pais.Codigo).ToListAsync(cancellationToken);
        return new ListasRisco(ibans.ToHashSet(StringComparer.Ordinal), paises.ToHashSet(StringComparer.Ordinal));
    }

    public Task<IbanBloqueado?> ObterIbanAsync(string iban, CancellationToken cancellationToken)
    {
        var normalizado = NormalizacaoIban.Normalizar(iban);
        return contexto.IbansBloqueados.SingleOrDefaultAsync(bloqueado => bloqueado.Iban == normalizado, cancellationToken);
    }

    public Task<PaisRisco?> ObterPaisAsync(string codigo, CancellationToken cancellationToken)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        return contexto.PaisesRisco.SingleOrDefaultAsync(pais => pais.Codigo == normalizado, cancellationToken);
    }

    public void Adicionar(IbanBloqueado iban) => contexto.IbansBloqueados.Add(iban);

    public void Adicionar(PaisRisco pais) => contexto.PaisesRisco.Add(pais);

    public void Remover(IbanBloqueado iban) => contexto.IbansBloqueados.Remove(iban);

    public void Remover(PaisRisco pais) => contexto.PaisesRisco.Remove(pais);
}
