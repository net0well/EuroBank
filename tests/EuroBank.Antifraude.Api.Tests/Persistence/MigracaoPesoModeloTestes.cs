using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EuroBank.Antifraude.Api.Tests.Persistence;

/// <summary>
/// O peso padrão do modelo passa a 50: num banco que já existia com peso 0, a migration cria uma versão nova da
/// configuração com os mesmos limiares; com outro peso (escolhido pelo analista), não mexe.
/// </summary>
public sealed class MigracaoPesoModeloTestes(AntifraudeFixture antifraude)
{
    private const string AntesDoPeso = "20261115040551_VersoesModelo";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0, 2)]
    [InlineData(30, 1)]
    public async Task BancoExistente_PesoZeroGanhaVersaoCom50(int pesoAntes, int versoesDepois)
    {
        var conexao = new NpgsqlConnectionStringBuilder(antifraude.ConnectionString) { Database = $"migracao_{Guid.NewGuid():N}" }.ConnectionString;
        await using var contexto = new AntifraudeDbContext(new DbContextOptionsBuilder<AntifraudeDbContext>().UseNpgsql(conexao).Options);
        try
        {
            var migrador = contexto.GetService<IMigrator>();
            await migrador.MigrateAsync(AntesDoPeso, Ct);
            await contexto.Database.ExecuteSqlAsync(
                $"INSERT INTO configuracoes_decisao (versao, limiar_revisao, limiar_recusa, peso_modelo, criada_em) VALUES (1, 40, 80, {pesoAntes}, now())",
                Ct);

            await migrador.MigrateAsync(cancellationToken: Ct);

            var versoes = await contexto.Configuracoes.AsNoTracking().OrderBy(configuracao => configuracao.Versao).ToListAsync(Ct);
            versoes.Count.ShouldBe(versoesDepois);
            versoes[0].PesoModelo.ShouldBe(pesoAntes);
            var atual = versoes[^1];
            atual.LimiarRevisao.ShouldBe(40);
            atual.LimiarRecusa.ShouldBe(80);
            atual.PesoModelo.ShouldBe(pesoAntes == 0 ? 50 : pesoAntes);
        }
        finally
        {
            await contexto.Database.EnsureDeletedAsync(Ct);
        }
    }
}
