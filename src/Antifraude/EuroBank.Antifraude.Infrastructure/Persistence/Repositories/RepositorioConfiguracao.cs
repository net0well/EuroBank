using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Repositories;

public sealed class RepositorioConfiguracao(AntifraudeDbContext contexto) : IRepositorioConfiguracao
{
    public Task<ConfiguracaoDecisao?> AtualAsync(CancellationToken cancellationToken) =>
        contexto.Configuracoes.OrderByDescending(configuracao => configuracao.Versao).FirstOrDefaultAsync(cancellationToken);

    public void Adicionar(ConfiguracaoDecisao configuracao) => contexto.Configuracoes.Add(configuracao);
}
