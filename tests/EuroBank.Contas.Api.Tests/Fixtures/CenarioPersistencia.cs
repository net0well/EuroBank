using EuroBank.Contas.Infrastructure.Outbox;
using EuroBank.Contas.Infrastructure.Persistence;
using EuroBank.Contas.Infrastructure.Persistence.Repositories;
using EuroBank.Shared.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Monta contexto, repositório, gerador de IBAN e unidade de trabalho como a API faz por requisição.</summary>
public sealed class CenarioPersistencia : IAsyncDisposable
{
    public CenarioPersistencia(PostgresFixture banco, bool apagarOutboxAposGravar = false)
        : this(banco.CriarContexto(), apagarOutboxAposGravar)
    {
    }

    /// <summary>Cenário sobre outro database (ex.: o da API de testes).</summary>
    public CenarioPersistencia(string connectionString)
        : this(new ContasDbContext(new DbContextOptionsBuilder<ContasDbContext>().UseNpgsql(connectionString).Options), false)
    {
    }

    private CenarioPersistencia(ContasDbContext contexto, bool apagarOutboxAposGravar)
    {
        Contexto = contexto;
        Fila = new FilaOutbox();
        Repositorio = new RepositorioContas(Contexto);
        GeradorIban = new GeradorIban(Contexto);
        UnidadeDeTrabalho = new UnidadeDeTrabalhoComOutbox<ContasDbContext>(
            Contexto,
            new MapeadorMensagensOutbox(),
            Fila,
            Options.Create(new OpcoesOutbox { ApagarAposGravar = apagarOutboxAposGravar }));
    }

    public ContasDbContext Contexto { get; }

    public FilaOutbox Fila { get; }

    public RepositorioContas Repositorio { get; }

    public GeradorIban GeradorIban { get; }

    public UnidadeDeTrabalhoComOutbox<ContasDbContext> UnidadeDeTrabalho { get; }

    public ValueTask DisposeAsync() => Contexto.DisposeAsync();
}
