using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;
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
            extrato.Moeda.ToString(),
            extrato.UltimaVersao,
            paginacao.Pagina,
            paginacao.Tamanho,
            total,
            pagina.Select(ParaDto).ToList());
    }

    /// <summary>No envio, o outro lado do câmbio é o destino; no recebimento, a origem.</summary>
    private static LancamentoDto ParaDto(Lancamento lancamento)
    {
        var recebimento = lancamento.Tipo == TipoLancamento.TransferenciaCambioRecebida;
        var cambio = lancamento.Cambio;
        return new LancamentoDto(
            lancamento.Tipo.ToString(),
            lancamento.Valor.Valor,
            lancamento.Tarifa?.Valor,
            lancamento.SaldoApos.Valor,
            lancamento.Versao,
            lancamento.OcorridoEm,
            lancamento.Descricao,
            MoedaDestino: recebimento ? null : cambio?.Moeda.ToString(),
            ValorDestino: recebimento ? null : cambio?.Valor,
            MoedaOrigem: recebimento ? cambio?.Moeda.ToString() : null,
            ValorOrigem: recebimento ? cambio?.Valor : null,
            Cotacao: cambio?.Cotacao);
    }
}
