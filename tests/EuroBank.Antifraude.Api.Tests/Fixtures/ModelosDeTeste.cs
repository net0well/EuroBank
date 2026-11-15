using System.Diagnostics;
using EuroBank.Antifraude.Infrastructure.MachineLearning;
using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Generation;
using EuroBank.Antifraude.Treinamento.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>
/// Um modelo treinado de verdade (mundo sintético pequeno, treinado uma vez por execução), o registro de versões no
/// banco da API e a espera pela troca em memória, que chega pelo LISTEN/NOTIFY.
/// </summary>
public sealed class ModelosDeTeste(AntifraudeFixture antifraude)
{
    public const int SementeDados = 7;

    private static readonly Lazy<ResultadoTreino> Treinado = new(() =>
    {
        var mundo = new OpcoesGeracao(Semente: SementeDados, Contas: 400, Dias: 60);
        var linhas = new GeradorTransferencias(mundo).Gerar()
            .Select(transferencia => new LinhaTreino(transferencia.Momento, transferencia.Features, transferencia.Fraude, Real: false))
            .ToList();
        return new TreinadorModelo(new OpcoesTreino(Arvores: 50)).Treinar(SementeDados, linhas, []);
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static ResultadoTreino Resultado => Treinado.Value;

    /// <summary>Os modelos em memória na API in-process.</summary>
    public ModelosEmUso EmUso => antifraude.Api.Services.GetRequiredService<PontuadorFraude>().EmUso;

    /// <summary>Registra uma versão (com o arquivo treinado ou outro, para simular um arquivo ruim); devolve o número.</summary>
    public async Task<int> RegistrarAsync(byte[]? arquivo = null)
    {
        await using var contexto = antifraude.CriarContexto();
        var versao = await RegistroModelos.RegistrarAsync(
            contexto, Resultado with { Arquivo = arquivo ?? Resultado.Arquivo }, DateTimeOffset.UtcNow, Ct);
        return versao.Versao;
    }

    /// <summary>Espera a API trocar os modelos em memória (o aviso chega depois que a transação confirma).</summary>
    public async Task EsperarAsync(Func<ModelosEmUso, bool> condicao, string descricao)
    {
        var relogio = Stopwatch.StartNew();
        while (!condicao(EmUso))
        {
            if (relogio.Elapsed > TimeSpan.FromSeconds(15))
            {
                throw new TimeoutException($"A API não chegou a {descricao}: ativa {EmUso.Ativo?.Versao}, challenger {EmUso.Challenger?.Versao}.");
            }

            await Task.Delay(50, Ct);
        }
    }

    /// <summary>Aposenta tudo direto no banco (o gatilho avisa a API) e espera a memória esvaziar.</summary>
    public async Task LimparAsync()
    {
        await using (var contexto = antifraude.CriarContexto())
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "UPDATE versoes_modelo SET status = 'Aposentada', aposentada_em = now() WHERE status IN ('Ativa', 'Challenger')", Ct);
        }

        await EsperarAsync(emUso => emUso.Ativo is null && emUso.Challenger is null, "nenhum modelo em uso");
    }
}
