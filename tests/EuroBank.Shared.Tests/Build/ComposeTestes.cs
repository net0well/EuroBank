using System.Text.RegularExpressions;

namespace EuroBank.Shared.Tests.Build;

/// <summary>
/// Com o profile ha, as aplicações conectam no Redis pelos sentinels, trocando só REDIS_CONEXAO. Um serviço com a
/// conexão escrita direto continuaria no primário antigo depois do failover, e só o laboratório mostraria.
/// </summary>
public sealed partial class ComposeTestes
{
    [Fact]
    public void ConexoesRedis_PassamPorRedisConexao()
    {
        var compose = File.ReadAllText(Path.Combine(RaizRepositorio.Caminho, "docker-compose.yml"));
        var conexoes = ConexaoRedis().Matches(compose).Select(conexao => conexao.Groups[1].Value).ToList();

        conexoes.ShouldNotBeEmpty();
        conexoes.ShouldAllBe(conexao => conexao == "\"${REDIS_CONEXAO:-redis:6379}\"");
    }

    [GeneratedRegex(@"ConnectionStrings__Redis:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex ConexaoRedis();
}
