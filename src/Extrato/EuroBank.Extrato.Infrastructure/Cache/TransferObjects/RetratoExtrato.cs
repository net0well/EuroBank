using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Options;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Infrastructure.Cache.TransferObjects;

/// <summary>
/// O resumo e os lançamentos mais recentes de uma conta, do mais recente para o mais antigo, como o Postgres os
/// devolve ou o Redis os guarda. GravadoEm é quando o retrato foi gravado no Redis (nulo quando veio do Postgres).
/// </summary>
public sealed record RetratoExtrato(
    EstadoExtrato Estado,
    long Total,
    IReadOnlyList<LancamentoDto> Lancamentos,
    DateTimeOffset? GravadoEm = null)
{
    /// <summary>
    /// A página pedida, se ela estiver inteira no retrato (dentro dos lançamentos guardados, ou a conta tem só esses);
    /// senão, nulo, e quem chama lê do Postgres.
    /// </summary>
    public ExtratoDto? ParaDto(Paginacao paginacao)
    {
        ArgumentNullException.ThrowIfNull(paginacao);
        var cabe = paginacao.Saltar + paginacao.Tamanho <= OpcoesCache.LancamentosNoCache
            || Total <= OpcoesCache.LancamentosNoCache;
        if (!cabe)
        {
            return null;
        }

        return new ExtratoDto(
            Estado.ContaId,
            Estado.Iban,
            Estado.Titular,
            Estado.Status,
            Estado.Saldo,
            Estado.Moeda,
            Estado.Versao,
            paginacao.Pagina,
            paginacao.Tamanho,
            Total,
            Lancamentos.Skip(paginacao.Saltar).Take(paginacao.Tamanho).ToList());
    }
}
