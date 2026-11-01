using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Infrastructure.Persistence;

/// <summary>Usa a sequence do Postgres para numerar contas sem colisão, mesmo com várias instâncias da API.</summary>
public sealed class GeradorIban(ContasDbContext contexto) : IGeradorIban
{
    public async Task<Iban> GerarAsync(CancellationToken cancellationToken)
    {
        var sequencial = await contexto.Database
            .SqlQueryRaw<long>($"SELECT nextval('{ContasDbContext.SequenciaNumeroConta}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return Iban.Gerar(sequencial);
    }
}
