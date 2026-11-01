using EuroBank.Contas.Api.Tests.Fixtures;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Api.Tests.Persistence;

public sealed class PersistenciaContasTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GeradorIban_GeraIbansDistintosEValidosEmSequencia()
    {
        await using var cenario = new CenarioPersistencia(banco);

        var primeiro = await cenario.GeradorIban.GerarAsync(Ct);
        var segundo = await cenario.GeradorIban.GerarAsync(Ct);

        primeiro.ShouldNotBe(segundo);
        Iban.EhValido(primeiro.Valor).ShouldBeTrue();
        Iban.EhValido(segundo.Valor).ShouldBeTrue();
    }

    [Fact]
    public async Task ObterPorIban_EncontraAContaGravada()
    {
        var conta = await CriarContaAsync(100m);

        await using var cenario = new CenarioPersistencia(banco);
        var encontrada = await cenario.Repositorio.ObterPorIbanAsync(conta.Iban, Ct);

        encontrada.ShouldNotBeNull();
        encontrada.Id.ShouldBe(conta.Id);
        encontrada.Saldo.ShouldBe(Dinheiro.De(100m));
    }

    [Fact]
    public async Task DuasOperacoesConcorrentes_ASegundaRecebeExcecaoConcorrenciaSemGravarOutbox()
    {
        var conta = await CriarContaAsync(100m);
        await using var primeira = new CenarioPersistencia(banco);
        await using var segunda = new CenarioPersistencia(banco);
        var contaPrimeira = await primeira.Repositorio.ObterPorIdAsync(conta.Id, Ct);
        var contaSegunda = await segunda.Repositorio.ObterPorIdAsync(conta.Id, Ct);

        contaPrimeira!.Sacar(Dinheiro.De(10m), Agora);
        await primeira.UnidadeDeTrabalho.SalvarAsync(Ct);
        contaSegunda!.Sacar(Dinheiro.De(20m), Agora);

        await Should.ThrowAsync<ExcecaoConcorrencia>(() => segunda.UnidadeDeTrabalho.SalvarAsync(Ct));

        await using var leitura = banco.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == conta.Id, Ct)).Saldo.ShouldBe(Dinheiro.De(90m));
        (await leitura.Outbox.CountAsync(m => m.IdAgregado == conta.Id.ToString() && m.TipoEvento == "SaqueRealizado", Ct))
            .ShouldBe(1);
    }

    [Fact]
    public async Task TabelaContas_TemReplicaIdentityFullParaOCdcBruto()
    {
        await using var leitura = banco.CriarContexto();

        var identidade = await leitura.Database
            .SqlQuery<string>($"SELECT relreplident::text AS \"Value\" FROM pg_class WHERE relname = 'contas'")
            .SingleAsync(Ct);

        identidade.ShouldBe("f");
    }

    private async Task<Conta> CriarContaAsync(decimal saldo)
    {
        await using var cenario = new CenarioPersistencia(banco);
        var conta = Conta.Abrir(await cenario.GeradorIban.GerarAsync(Ct), Titular.Criar("Fábio"), Agora);
        conta.Depositar(Dinheiro.De(saldo), Agora);
        cenario.Repositorio.Adicionar(conta);
        await cenario.UnidadeDeTrabalho.SalvarAsync(Ct);
        return conta;
    }
}
