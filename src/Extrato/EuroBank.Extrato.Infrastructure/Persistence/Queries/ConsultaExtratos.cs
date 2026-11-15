using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Mappers;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Persistence.Queries;

/// <summary>O extrato lido direto do Postgres: a página pedida, ou o retrato que o cache guarda.</summary>
public sealed class ConsultaExtratos(ExtratoDbContext contexto) : IConsultaExtratos
{
    public async Task<ExtratoDto?> ObterAsync(Guid contaId, Paginacao paginacao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginacao);
        var retrato = await LerAsync(contaId, paginacao.Saltar, paginacao.Tamanho, cancellationToken);
        return retrato is null
            ? null
            : new ExtratoDto(
                retrato.Estado.ContaId,
                retrato.Estado.Iban,
                retrato.Estado.Titular,
                retrato.Estado.Status,
                retrato.Estado.Saldo,
                retrato.Estado.Moeda,
                retrato.Estado.Versao,
                paginacao.Pagina,
                paginacao.Tamanho,
                retrato.Total,
                retrato.Lancamentos);
    }

    /// <summary>O resumo, o total e os <paramref name="quantidade"/> lançamentos mais recentes, para gravar no Redis.</summary>
    public Task<RetratoExtrato?> ObterRetratoAsync(Guid contaId, int quantidade, CancellationToken cancellationToken) =>
        LerAsync(contaId, saltar: 0, quantidade, cancellationToken);

    private async Task<RetratoExtrato?> LerAsync(Guid contaId, int saltar, int quantidade, CancellationToken cancellationToken)
    {
        var extrato = await contexto.Extratos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ContaId == contaId, cancellationToken);
        if (extrato is null)
        {
            return null;
        }

        var lancamentosDaConta = contexto.Lancamentos
            .AsNoTracking()
            .Where(lancamento => EF.Property<Guid>(lancamento, "ContaId") == contaId);
        var total = await lancamentosDaConta.LongCountAsync(cancellationToken);
        var pagina = await lancamentosDaConta
            .OrderByDescending(lancamento => lancamento.Versao)
            .Skip(saltar)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

        return new RetratoExtrato(EstadoExtrato.De(extrato), total, pagina.Select(LancamentoMapeador.ParaDto).ToList());
    }
}
