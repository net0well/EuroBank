namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// Scripts Lua do cache do Extrato. Cada um roda atômico no Redis e só avança o resumo com uma versão da conta maior
/// que a guardada: duas escritas fora de ordem (um retrato lido antes de um evento, um evento repetido) não fazem o
/// cache voltar no tempo.
/// </summary>
public static class ScriptsCache
{
    /// <summary>
    /// Aplica o estado depois de um evento e os lançamentos novos. KEYS: resumo, lançamentos, pendências. ARGV: versão,
    /// exigir existente (1 ou 0), TTL em ms (0 sem TTL), gravado em, pendência (vazia: sem stream), limite da lista,
    /// os sete campos do resumo e os lançamentos novos (do mais antigo para o mais recente).
    /// Retorna -1 se exigia a entrada e ela não existe, 0 se a versão não é nova, 1 se aplicou.
    /// </summary>
    public const string Aplicar = """
        local atual = redis.call('HGET', KEYS[1], 'versao')
        if not atual then
          if ARGV[2] == '1' then return -1 end
        elseif tonumber(atual) >= tonumber(ARGV[1]) then
          return 0
        end
        redis.call('HSET', KEYS[1], 'versao', ARGV[1], 'gravado_em', ARGV[4], 'conta_id', ARGV[7], 'iban', ARGV[8],
          'titular', ARGV[9], 'status', ARGV[10], 'moeda', ARGV[11], 'saldo', ARGV[12], 'atualizado_em', ARGV[13])
        local novos = #ARGV - 13
        for i = 14, #ARGV do
          redis.call('LPUSH', KEYS[2], ARGV[i])
        end
        redis.call('LTRIM', KEYS[2], 0, tonumber(ARGV[6]) - 1)
        redis.call('HINCRBY', KEYS[1], 'total', novos)
        if tonumber(ARGV[3]) > 0 then
          redis.call('PEXPIRE', KEYS[1], ARGV[3])
          redis.call('PEXPIRE', KEYS[2], ARGV[3])
        end
        if ARGV[5] ~= '' then
          redis.call('XADD', KEYS[3], '*', 'dados', ARGV[5])
        end
        return 1
        """;

    /// <summary>
    /// Grava o retrato lido do Postgres (resumo, total e os lançamentos mais recentes), se a versão dele for maior que a
    /// guardada. KEYS: resumo, lançamentos. ARGV: versão, TTL em ms (0 sem TTL), gravado em, total, os sete campos do
    /// resumo e os lançamentos (do mais recente para o mais antigo). Retorna 1 se gravou, 0 se já havia um mais novo.
    /// </summary>
    public const string GravarRetrato = """
        local atual = redis.call('HGET', KEYS[1], 'versao')
        if atual and tonumber(atual) >= tonumber(ARGV[1]) then return 0 end
        redis.call('DEL', KEYS[2])
        redis.call('HSET', KEYS[1], 'versao', ARGV[1], 'gravado_em', ARGV[3], 'total', ARGV[4], 'conta_id', ARGV[5],
          'iban', ARGV[6], 'titular', ARGV[7], 'status', ARGV[8], 'moeda', ARGV[9], 'saldo', ARGV[10], 'atualizado_em', ARGV[11])
        if #ARGV >= 12 then
          redis.call('RPUSH', KEYS[2], unpack(ARGV, 12, #ARGV))
        end
        if tonumber(ARGV[2]) > 0 then
          redis.call('PEXPIRE', KEYS[1], ARGV[2])
          redis.call('PEXPIRE', KEYS[2], ARGV[2])
        end
        return 1
        """;
}
