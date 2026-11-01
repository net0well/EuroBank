using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Infrastructure.Persistence.Queries;

public sealed class ConsultaContas(ContasDbContext contexto) : IConsultaContas
{
    public async Task<ResultadoPaginado<ContaDto>> ListarAsync(Paginacao paginacao, CancellationToken cancellationToken)
    {
        var total = await contexto.Contas.LongCountAsync(cancellationToken);
        var contas = await contexto.Contas
            .AsNoTracking()
            .OrderByDescending(conta => conta.AbertaEm)
            .ThenByDescending(conta => conta.Id)
            .Skip(paginacao.Saltar)
            .Take(paginacao.Tamanho)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<ContaDto>(
            contas.Select(conta => conta.ParaDto()).ToList(), paginacao.Pagina, paginacao.Tamanho, total);
    }
}
