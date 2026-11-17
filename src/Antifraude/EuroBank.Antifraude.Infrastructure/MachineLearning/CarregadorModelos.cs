using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Põe em memória as versões que o banco diz estarem ativa e em sombra. Só lê o arquivo de uma versão que ainda não
/// está carregada (o challenger promovido a ativo é reaproveitado). Uma versão que não carrega não derruba nada:
/// fica a que estava naquele papel, e o erro vai para o log e para a métrica de falhas.
/// </summary>
public sealed class CarregadorModelos(
    IServiceScopeFactory escopos,
    PontuadorFraude pontuador,
    TelemetriaModelo telemetria,
    ILogger<CarregadorModelos> logger)
{
    private readonly SemaphoreSlim _umPorVez = new(1, 1);

    public async Task RecarregarAsync(CancellationToken cancellationToken)
    {
        await _umPorVez.WaitAsync(cancellationToken);
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<AntifraudeDbContext>();
            var emUso = await contexto.VersoesModelo.AsNoTracking()
                .Where(versao => versao.Status == StatusModelo.Ativa || versao.Status == StatusModelo.Challenger)
                .Select(versao => new { versao.Versao, versao.Status })
                .ToListAsync(cancellationToken);
            var ativa = emUso.SingleOrDefault(versao => versao.Status == StatusModelo.Ativa)?.Versao;
            var challenger = emUso.SingleOrDefault(versao => versao.Status == StatusModelo.Challenger)?.Versao;

            var anterior = pontuador.EmUso;
            var novos = new ModelosEmUso(
                await ResolverAsync(contexto, ativa, anterior, anterior.Ativo, PontuadorFraude.PapelAtiva, cancellationToken),
                await ResolverAsync(contexto, challenger, anterior, anterior.Challenger, PontuadorFraude.PapelChallenger, cancellationToken));
            pontuador.Trocar(novos);

            if (novos.Ativo?.Versao != anterior.Ativo?.Versao || novos.Challenger?.Versao != anterior.Challenger?.Versao)
            {
                logger.LogInformation(
                    "Modelos em uso: ativa {Ativa}, challenger {Challenger}",
                    novos.Ativo?.Versao.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "nenhuma",
                    novos.Challenger?.Versao.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "nenhum");
            }
        }
        finally
        {
            _umPorVez.Release();
        }
    }

    private async Task<ModeloCarregado?> ResolverAsync(
        AntifraudeDbContext contexto,
        int? versao,
        ModelosEmUso anterior,
        ModeloCarregado? noPapel,
        string papel,
        CancellationToken cancellationToken)
    {
        if (versao is null)
        {
            return null;
        }

        if (anterior.Ativo?.Versao == versao)
        {
            return anterior.Ativo;
        }

        if (anterior.Challenger?.Versao == versao)
        {
            return anterior.Challenger;
        }

        var arquivo = await contexto.VersoesModelo.AsNoTracking()
            .Where(modelo => modelo.Versao == versao)
            .Select(modelo => modelo.Arquivo)
            .SingleAsync(cancellationToken);
        try
        {
            var carregado = ModeloCarregado.Carregar(versao.Value, arquivo);
            logger.LogInformation("Versão {Versao} do modelo carregada ({Papel}, {Bytes} bytes)", versao, papel, arquivo.Length);
            return carregado;
        }
        catch (InvalidDataException erro)
        {
            telemetria.Falha(papel, "carga");
            logger.LogError(
                erro,
                "A versão {Versao} do modelo não carregou ({Papel}); fica a versão {Anterior}",
                versao,
                papel,
                noPapel?.Versao.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "nenhuma");
            return noPapel;
        }
    }
}
