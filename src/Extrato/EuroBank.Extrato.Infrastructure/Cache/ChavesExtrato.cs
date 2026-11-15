using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Chaves do Extrato no Redis: por conta, o resumo (hash) e os últimos lançamentos (lista, do mais recente para o mais
/// antigo); para o write-behind, o stream das pendências que ainda vão para o Postgres.
/// </summary>
public static class ChavesExtrato
{
    public const string PadraoResumo = "extrato:*:resumo";

    public const string PadraoLancamentos = "extrato:*:lancamentos";

    public const string GrupoDescarga = "descarga";

    public static readonly RedisKey Pendencias = "extrato:pendencias";

    public static RedisKey Resumo(Guid contaId) => $"extrato:{{{contaId}}}:resumo";

    public static RedisKey Lancamentos(Guid contaId) => $"extrato:{{{contaId}}}:lancamentos";
}
