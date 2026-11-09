using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.TransferObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Transferencias.Infrastructure.Persistence.Queries;

public sealed class ConsultaTransferencias(TransferenciasDbContext contexto) : IConsultaTransferencias
{
    public async Task<ResultadoPaginado<TransferenciaResumoDto>> ListarAsync(
        FiltroTransferencias filtro,
        Paginacao paginacao,
        CancellationToken cancellationToken)
    {
        // A listagem não mostra o histórico: sem o AutoInclude dos passos.
        var consulta = contexto.Transferencias.AsNoTracking().IgnoreAutoIncludes();
        if (filtro.ContaOrigemId is { } conta)
        {
            consulta = consulta.Where(transferencia => transferencia.ContaOrigemId == conta);
        }

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(transferencia => transferencia.Tipo == tipo);
        }

        if (filtro.Estado is { } estado)
        {
            consulta = consulta.Where(transferencia => transferencia.Estado == estado);
        }

        var total = await consulta.LongCountAsync(cancellationToken);
        var transferencias = await consulta
            .OrderByDescending(transferencia => transferencia.SolicitadaEm)
            .ThenByDescending(transferencia => transferencia.Id)
            .Skip(paginacao.Saltar)
            .Take(paginacao.Tamanho)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<TransferenciaResumoDto>(
            transferencias.Select(transferencia => transferencia.ParaResumo()).ToList(),
            paginacao.Pagina,
            paginacao.Tamanho,
            total);
    }
}
