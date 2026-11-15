using StackExchange.Redis;

namespace EuroBank.Extrato.Infrastructure.Cache;

/// <summary>
/// O Redis fora do ar ou lento: conexão recusada, comando que não respondeu a tempo. O timeout do StackExchange.Redis
/// não deriva de RedisException, por isso as duas.
/// </summary>
public static class FalhaRedis
{
    public static bool Eh(Exception erro) => erro is RedisException or RedisTimeoutException;
}
