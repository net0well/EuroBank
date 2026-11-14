using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Policies;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados iniciais que vêm do código: as regras do catálogo que ainda não existem no banco (uma regra nova entra
/// sozinha na próxima subida, sem mexer nas já ajustadas) e a configuração de decisão inicial. As listas de risco
/// iniciais vêm da migration, para não voltarem depois de um analista removê-las.
/// </summary>
public sealed class SemeadorAntifraude(AntifraudeDbContext contexto, TimeProvider relogio)
{
    public async Task SemearAsync(CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();
        var existentes = await contexto.Regras.Select(regra => regra.Codigo).ToListAsync(cancellationToken);
        foreach (var definicao in CatalogoRegras.Definicoes.Where(definicao => !existentes.Contains(definicao.Codigo)))
        {
            contexto.Regras.Add(Regra.Criar(definicao, agora));
        }

        if (!await contexto.Configuracoes.AnyAsync(cancellationToken))
        {
            contexto.Configuracoes.Add(ConfiguracaoDecisao.Inicial(agora));
        }

        await contexto.SaveChangesAsync(cancellationToken);
    }
}
