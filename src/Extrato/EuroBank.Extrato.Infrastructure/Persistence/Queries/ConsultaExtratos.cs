using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Persistence.Queries;

public sealed class ConsultaExtratos(ExtratoDbContext contexto) : IConsultaExtratos
{
    public async Task<ExtratoDto?> ObterAsync(Guid contaId, Paginacao paginacao, CancellationToken cancellationToken)
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
            .Skip(paginacao.Saltar)
            .Take(paginacao.Tamanho)
            .ToListAsync(cancellationToken);

        return new ExtratoDto(
            extrato.ContaId,
            extrato.Iban,
            extrato.Titular,
            extrato.Status.ToString(),
            extrato.SaldoAtual.Valor,
            Dinheiro.Moeda,
            extrato.UltimaVersao,
            paginacao.Pagina,
            paginacao.Tamanho,
            total,
            pagina.Select(lancamento => new LancamentoDto(
                    lancamento.Tipo.ToString(),
                    lancamento.Valor.Valor,
                    lancamento.Tarifa?.Valor,
                    lancamento.SaldoApos.Valor,
                    lancamento.Versao,
                    lancamento.OcorridoEm,
                    lancamento.Descricao))
                .ToList());
    }
}
