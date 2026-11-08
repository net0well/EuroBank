using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Persistence.Outbox;
using Microsoft.Extensions.Options;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Monta contexto, repositório, gerador de IBAN e unidade de trabalho como a API faz por requisição.</summary>
public sealed class CenarioPersistencia : IAsyncDisposable
{
    public CenarioPersistencia(PostgresFixture banco, bool apagarOutboxAposGravar = false)
    {
        Contexto = banco.CriarContexto();
        Repositorio = new RepositorioContas(Contexto);
        GeradorIban = new GeradorIban(Contexto);
        UnidadeDeTrabalho = new UnidadeDeTrabalhoComOutbox<ContasDbContext>(
            Contexto,
            new MapeadorMensagensOutbox(),
            Options.Create(new OpcoesOutbox { ApagarAposGravar = apagarOutboxAposGravar }));
    }

    public ContasDbContext Contexto { get; }

    public RepositorioContas Repositorio { get; }

    public GeradorIban GeradorIban { get; }

    public UnidadeDeTrabalhoComOutbox<ContasDbContext> UnidadeDeTrabalho { get; }

    public ValueTask DisposeAsync() => Contexto.DisposeAsync();
}
