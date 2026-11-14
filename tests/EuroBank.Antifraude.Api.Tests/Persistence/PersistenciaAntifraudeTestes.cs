using System.Text.Json;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Persistence.Seed;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Api.Tests.Persistence;

/// <summary>Análises, casos, regras, configuração e listas no Postgres, com os eventos na outbox.</summary>
public sealed class PersistenciaAntifraudeTestes(PostgresFixture banco)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ContextoOperacao Operacao(string iban = "DE89370400440532013000") =>
        new(Guid.CreateVersion7(), TipoOperacao.Externa, Guid.CreateVersion7(), Dinheiro.De(250.75m, Moeda.USD), iban, Agora);

    private static Analise NovaAnalise(ContextoOperacao operacao, ListasRisco? listas = null)
    {
        var perfil = PerfilConta.Vazio(operacao.ContaId);
        var resultado = new MotorDecisao().Decidir(
            operacao, perfil, CatalogoRegras.RegrasIniciais(Agora), listas ?? ListasRisco.Vazias, ConfiguracaoDecisao.Inicial(Agora));
        return Analise.Registrar(operacao, resultado, Agora);
    }

    private async Task SalvarAsync(Action<IServiceProvider> preparar) =>
        await banco.EmEscopoAsync(async servicos =>
        {
            preparar(servicos);
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });

    [Fact]
    public async Task Analise_VoltaComOContextoEOsSinais()
    {
        var operacao = Operacao();
        var analise = NovaAnalise(operacao);
        await SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioAnalises>().Adicionar(analise));

        Analise? lida = null;
        await banco.EmEscopoAsync(async servicos =>
            lida = await servicos.GetRequiredService<IRepositorioAnalises>().ObterPorOperacaoAsync(operacao.OperacaoId, Ct));

        lida.ShouldNotBeNull();
        lida.Id.ShouldBe(analise.Id);
        lida.Contexto.ShouldBe(operacao);
        lida.Sinais.ShouldBe(analise.Sinais);
        lida.ScoreFinal.ShouldBe(analise.ScoreFinal);
        lida.Decisao.ShouldBe(analise.Decisao);
        lida.VersaoConfiguracao.ShouldBe(1);
    }

    [Fact]
    public async Task AnaliseDecididaPeloMotor_VaiParaAOutbox()
    {
        var operacao = Operacao();
        var analise = NovaAnalise(operacao, new ListasRisco(new HashSet<string> { operacao.IbanDestino! }, new HashSet<string>()));
        await SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioAnalises>().Adicionar(analise));

        await using var contexto = banco.CriarContexto();
        var mensagem = await contexto.Outbox.SingleAsync(m => m.IdAgregado == operacao.OperacaoId.ToString(), Ct);
        mensagem.TipoAgregado.ShouldBe("antifraude.eventos");
        mensagem.TipoEvento.ShouldBe("AnaliseDecidida");
        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("analiseId").GetGuid().ShouldBe(analise.Id.Valor);
        payload.RootElement.GetProperty("operacaoId").GetGuid().ShouldBe(operacao.OperacaoId);
        payload.RootElement.GetProperty("contaId").GetGuid().ShouldBe(operacao.ContaId);
        payload.RootElement.GetProperty("decisao").GetString().ShouldBe("Recusada");
        payload.RootElement.GetProperty("origem").GetString().ShouldBe("Motor");
        payload.RootElement.GetProperty("scoreFinal").GetInt32().ShouldBe(analise.ScoreFinal);
    }

    [Fact]
    public async Task MesmaOperacaoDuasVezes_ASegundaGravacaoEhConflito()
    {
        var operacao = Operacao();
        await SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioAnalises>().Adicionar(NovaAnalise(operacao)));

        await Should.ThrowAsync<ExcecaoConcorrencia>(() =>
            SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioAnalises>().Adicionar(NovaAnalise(operacao))));
    }

    [Fact]
    public async Task Caso_VoltaComAAuditoria_EOCasoAbertoVaiParaAOutbox()
    {
        var operacaoId = Guid.CreateVersion7();
        var caso = CasoRevisao.Abrir(AnaliseId.Nova(), operacaoId, Guid.CreateVersion7(), 45, TimeSpan.FromMinutes(30), Agora);
        caso.Assumir("Marta", Agora.AddMinutes(1));
        await SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioCasos>().Adicionar(caso));

        CasoRevisao? lido = null;
        await banco.EmEscopoAsync(async servicos =>
            lido = await servicos.GetRequiredService<IRepositorioCasos>().ObterPorOperacaoAsync(operacaoId, Ct));

        lido.ShouldNotBeNull();
        lido.Estado.ShouldBe(EstadoCaso.EmAnalise);
        lido.Analista.ShouldBe("Marta");
        lido.ExpiraEm.ShouldBe(Agora.AddMinutes(30));
        lido.Auditoria.Select(registro => (registro.Ordem, registro.Quem, registro.Acao)).ShouldBe(
            [(1, "sistema", "Caso aberto"), (2, "Marta", "Caso assumido")]);

        await using var contexto = banco.CriarContexto();
        var mensagem = await contexto.Outbox.SingleAsync(m => m.IdAgregado == operacaoId.ToString(), Ct);
        mensagem.TipoEvento.ShouldBe("CasoAberto");
        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("casoId").GetGuid().ShouldBe(caso.Id.Valor);
        payload.RootElement.GetProperty("scoreFinal").GetInt32().ShouldBe(45);
    }

    [Fact]
    public async Task DuasAlteracoesNoMesmoCaso_AoMesmoTempo_UmaPerde()
    {
        var caso = CasoRevisao.Abrir(AnaliseId.Nova(), Guid.CreateVersion7(), Guid.CreateVersion7(), 45, TimeSpan.FromMinutes(30), Agora);
        await SalvarAsync(servicos => servicos.GetRequiredService<IRepositorioCasos>().Adicionar(caso));

        await using var escopoA = banco.Servicos.CreateAsyncScope();
        await using var escopoB = banco.Servicos.CreateAsyncScope();
        var casoA = await escopoA.ServiceProvider.GetRequiredService<IRepositorioCasos>().ObterAsync(caso.Id, Ct);
        var casoB = await escopoB.ServiceProvider.GetRequiredService<IRepositorioCasos>().ObterAsync(caso.Id, Ct);
        casoA!.Assumir("Marta", Agora.AddMinutes(1));
        casoB!.CancelarPeloCliente(Agora.AddMinutes(1));

        await escopoA.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        await Should.ThrowAsync<ExcecaoConcorrencia>(() =>
            escopoB.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct));
    }

    [Fact]
    public async Task Vencidos_SoOsPendentesComPrazoPassado()
    {
        var prazo = TimeSpan.FromMinutes(30);
        var momento = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var vencido = CasoRevisao.Abrir(AnaliseId.Nova(), Guid.CreateVersion7(), Guid.CreateVersion7(), 40, prazo, momento.AddHours(-2));
        var noPrazo = CasoRevisao.Abrir(AnaliseId.Nova(), Guid.CreateVersion7(), Guid.CreateVersion7(), 40, prazo, momento);
        var decidido = CasoRevisao.Abrir(AnaliseId.Nova(), Guid.CreateVersion7(), Guid.CreateVersion7(), 40, prazo, momento.AddHours(-2));
        decidido.CancelarPeloCliente(momento.AddHours(-2));
        await SalvarAsync(servicos =>
        {
            var casos = servicos.GetRequiredService<IRepositorioCasos>();
            casos.Adicionar(vencido);
            casos.Adicionar(noPrazo);
            casos.Adicionar(decidido);
        });

        IReadOnlyList<CasoRevisao> vencidos = [];
        await banco.EmEscopoAsync(async servicos =>
            vencidos = await servicos.GetRequiredService<IRepositorioCasos>().VencidosAsync(momento, 100, Ct));

        vencidos.Select(caso => caso.Id).ShouldContain(vencido.Id);
        vencidos.Select(caso => caso.Id).ShouldNotContain(noPrazo.Id);
        vencidos.Select(caso => caso.Id).ShouldNotContain(decidido.Id);
    }

    [Fact]
    public async Task DadosIniciais_RegrasConfiguracaoEListas_SemDuplicarAoSemearDeNovo()
    {
        await banco.EmEscopoAsync(servicos => servicos.GetRequiredService<SemeadorAntifraude>().SemearAsync(Ct));

        await using var contexto = banco.CriarContexto();
        (await contexto.Regras.CountAsync(Ct)).ShouldBe(CatalogoRegras.Definicoes.Count);
        (await contexto.Configuracoes.MinAsync(configuracao => configuracao.Versao, Ct)).ShouldBe(1);
        (await contexto.Configuracoes.CountAsync(configuracao => configuracao.Versao == 1, Ct)).ShouldBe(1);

        ListasRisco? listas = null;
        await banco.EmEscopoAsync(async servicos => listas = await servicos.GetRequiredService<IRepositorioListas>().ObterAsync(Ct));
        listas!.EhPaisDeRisco("IR").ShouldBeTrue();
        listas.EhPaisDeRisco("KP").ShouldBeTrue();
        listas.EhPaisDeRisco("MM").ShouldBeTrue();
        listas.EstaBloqueado("DE89370400440532013066").ShouldBeTrue();
    }

    [Fact]
    public async Task RegraAjustada_GuardaPesoEParametros()
    {
        await banco.EmEscopoAsync(async servicos =>
        {
            var regra = await servicos.GetRequiredService<IRepositorioRegras>().ObterAsync(CodigoRegra.TesteDeCartao, Ct);
            regra!.Ajustar(25, ativa: true, sombra: true, new Dictionary<string, decimal> { ["valorMaximo"] = 7.5m }, Agora);
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });

        Regra? lida = null;
        await banco.EmEscopoAsync(async servicos =>
            lida = await servicos.GetRequiredService<IRepositorioRegras>().ObterAsync(CodigoRegra.TesteDeCartao, Ct));

        lida!.Peso.ShouldBe(25);
        lida.Sombra.ShouldBeTrue();
        lida.Parametro("valorMaximo").ShouldBe(7.5m);
        lida.Parametro("quantidade").ShouldBe(3m);

        // Volta ao padrão para os outros testes.
        await banco.EmEscopoAsync(async servicos =>
        {
            var regra = await servicos.GetRequiredService<IRepositorioRegras>().ObterAsync(CodigoRegra.TesteDeCartao, Ct);
            regra!.Ajustar(20, ativa: true, sombra: false, new Dictionary<string, decimal> { ["valorMaximo"] = 10m }, Agora);
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });
    }

    [Fact]
    public async Task Listas_IncluirERemover()
    {
        const string iban = "PT50000201231234567890154";
        await SalvarAsync(servicos =>
        {
            var listas = servicos.GetRequiredService<IRepositorioListas>();
            listas.Adicionar(IbanBloqueado.Criar(iban, "teste", "Marta", Agora));
            listas.Adicionar(PaisRisco.Criar("VU", Agora));
        });

        await banco.EmEscopoAsync(async servicos =>
        {
            var listas = servicos.GetRequiredService<IRepositorioListas>();
            var lidas = await listas.ObterAsync(Ct);
            lidas.EstaBloqueado(iban).ShouldBeTrue();
            lidas.EhPaisDeRisco("VU").ShouldBeTrue();

            listas.Remover((await listas.ObterIbanAsync(iban, Ct))!);
            listas.Remover((await listas.ObterPaisAsync("vu", Ct))!);
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });

        await banco.EmEscopoAsync(async servicos =>
        {
            var lidas = await servicos.GetRequiredService<IRepositorioListas>().ObterAsync(Ct);
            lidas.EstaBloqueado(iban).ShouldBeFalse();
            lidas.EhPaisDeRisco("VU").ShouldBeFalse();
        });
    }

    [Fact]
    public async Task NovaConfiguracao_PassaASerAAtual()
    {
        await banco.EmEscopoAsync(async servicos =>
        {
            var repositorio = servicos.GetRequiredService<IRepositorioConfiguracao>();
            var atual = await repositorio.AtualAsync(Ct);
            repositorio.Adicionar(atual!.NovaVersao(atual.LimiarRevisao, atual.LimiarRecusa, atual.PesoModelo, Agora));
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });

        await using var contexto = banco.CriarContexto();
        var maior = await contexto.Configuracoes.MaxAsync(configuracao => configuracao.Versao, Ct);
        await banco.EmEscopoAsync(async servicos =>
            (await servicos.GetRequiredService<IRepositorioConfiguracao>().AtualAsync(Ct))!.Versao.ShouldBe(maior));
    }
}
