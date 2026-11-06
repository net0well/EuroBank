using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Persistence.Repositories;

public sealed class RepositorioExtratos(ExtratoDbContext contexto) : IRepositorioExtratos
{
    /// <summary>
    /// Carrega o extrato sem os lançamentos: aplicar um evento só depende da última versão e do saldo, e
    /// carregar o histórico inteiro a cada mensagem ficaria cada vez mais caro. Lançamentos novos entram como INSERT.
    /// </summary>
    public Task<ExtratoConta?> ObterAsync(Guid contaId, CancellationToken cancellationToken) =>
        contexto.Extratos.FirstOrDefaultAsync(extrato => extrato.ContaId == contaId, cancellationToken);

    public void Adicionar(ExtratoConta extrato) => contexto.Extratos.Add(extrato);
}
