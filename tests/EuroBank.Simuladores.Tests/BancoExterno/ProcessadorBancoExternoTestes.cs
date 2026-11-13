using System.Text.Json;
using EuroBank.Simuladores.BancoExterno;
using EuroBank.Simuladores.BancoExterno.Persistencia;
using EuroBank.Simuladores.BancoExterno.Regras;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace EuroBank.Simuladores.Tests.BancoExterno;

public sealed class BancoExternoFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("eurobank_banco_externo")
        .WithUsername("eurobank")
        .WithPassword("eurobank")
        .Build();

    public RepositorioBancoExterno Repositorio { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Repositorio = new RepositorioBancoExterno(_container.GetConnectionString());
        await Repositorio.CriarEstruturaAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

public sealed class ProcessadorBancoExternoTestes(BancoExternoFixture banco) : IClassFixture<BancoExternoFixture>
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly RepositorioBancoExterno _repositorio = banco.Repositorio;

    private readonly ProcessadorComandos _processador = new(
        banco.Repositorio,
        new RegrasLiquidacao(
            Options.Create(new OpcoesBancoExterno
            {
                LatenciaMs = 1000,
                AtrasoFinal97Segundos = 45,
                AtrasoFinal95Segundos = 300,
                AtrasoDevolucaoSegundos = 10,
            }),
            new Random(1)),
        new FakeTimeProvider(Agora),
        NullLogger<ProcessadorComandos>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task LiquidarAsync(Guid transferenciaId, string iban, string? idMensagem = null) =>
        _processador.ProcessarAsync(
            new MensagemRecebida(
                idMensagem ?? Guid.CreateVersion7().ToString(),
                "LiquidarTransferencia",
                transferenciaId.ToString(),
                JsonSerializer.Serialize(new
                {
                    transferenciaId,
                    ibanDestino = iban,
                    bic = "DEUTDEFF",
                    nomeBeneficiario = "Bruno",
                    valor = "100.00",
                    moeda = "EUR",
                }, JsonSerializerOptions.Web),
                "00-4ec0e9a4e59373e59c1b456265157e28-f6186d492757de83-01"),
            Ct);

    private Task ConsultarAsync(Guid transferenciaId) =>
        _processador.ProcessarAsync(
            new MensagemRecebida(
                Guid.CreateVersion7().ToString(),
                "ConsultarLiquidacao",
                transferenciaId.ToString(),
                JsonSerializer.Serialize(new { transferenciaId }, JsonSerializerOptions.Web),
                null),
            Ct);

    /// <summary>Roda a entrega e devolve só o que saiu desta transferência (a tabela é compartilhada entre os testes).</summary>
    private async Task<List<RespostaAgendada>> EntregarAsync(Guid transferenciaId, DateTimeOffset agora)
    {
        var entregues = new List<RespostaAgendada>();
        await _repositorio.EntregarVencidasAsync(
            agora,
            resposta =>
            {
                if (resposta.TransferenciaId == transferenciaId)
                {
                    entregues.Add(resposta);
                }

                return Task.CompletedTask;
            },
            Ct);
        return entregues;
    }

    [Fact]
    public async Task IbanComum_LiquidaEAgendaConfirmacaoComALatencia()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013000");

        (await _repositorio.ObterStatusAsync(id, Ct)).ShouldBe("Liquidada");
        var resposta = (await _repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem();
        resposta.Tipo.ShouldBe("LiquidacaoConfirmada");
        resposta.ResponderEm.ShouldBe(Agora.AddSeconds(1));
        resposta.Traceparent.ShouldNotBeNull();
    }

    [Fact]
    public async Task Final99_RecusaEAgendaARecusa()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013099");

        (await _repositorio.ObterStatusAsync(id, Ct)).ShouldBe("Recusada");
        (await _repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem().Tipo.ShouldBe("LiquidacaoRecusada");
    }

    [Fact]
    public async Task Final98_NuncaResponde_NemAConsulta()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013098");
        await ConsultarAsync(id);

        (await _repositorio.ListarRespostasAsync(id, Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Final97_CadaReenvioAgendaUmaRespostaAtrasada()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013097");
        await LiquidarAsync(id, "DE89370400440532013097");

        var respostas = await _repositorio.ListarRespostasAsync(id, Ct);
        respostas.Count.ShouldBe(2);
        respostas.ShouldAllBe(r => r.Tipo == "LiquidacaoConfirmada" && r.ResponderEm == Agora.AddSeconds(45));
    }

    [Fact]
    public async Task Final96_LiquidaSemResponder_MasAConsultaDizLiquidada()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013096");
        (await _repositorio.ListarRespostasAsync(id, Ct)).ShouldBeEmpty();
        await ConsultarAsync(id);

        var resposta = (await _repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem();
        resposta.Tipo.ShouldBe("StatusLiquidacao");
        using var payload = JsonDocument.Parse(resposta.Payload);
        payload.RootElement.GetProperty("status").GetString().ShouldBe("Liquidada");
    }

    [Fact]
    public async Task Final95_ConsultaDizNaoLiquidada_MasAConfirmacaoChegaDepois()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013095");
        await LiquidarAsync(id, "DE89370400440532013095");
        await ConsultarAsync(id);

        var respostas = await _repositorio.ListarRespostasAsync(id, Ct);
        respostas.Count.ShouldBe(2);
        using (var consulta = JsonDocument.Parse(respostas.Single(r => r.Tipo == "StatusLiquidacao").Payload))
        {
            consulta.RootElement.GetProperty("status").GetString().ShouldBe("NaoLiquidada");
        }

        // Uma confirmação só, mesmo com o reenvio, cinco minutos depois.
        respostas.Single(r => r.Tipo == "LiquidacaoConfirmada").ResponderEm.ShouldBe(Agora.AddSeconds(300));
    }

    [Fact]
    public async Task Final94_ConfirmaEDepoisDevolve()
    {
        var id = Guid.CreateVersion7();

        await LiquidarAsync(id, "DE89370400440532013094");

        var respostas = await _repositorio.ListarRespostasAsync(id, Ct);
        respostas.Select(r => r.Tipo).ShouldBe(["LiquidacaoConfirmada", "TransferenciaDevolvida"]);
        respostas[1].ResponderEm.ShouldBe(Agora.AddSeconds(11));
        using var devolucao = JsonDocument.Parse(respostas[1].Payload);
        devolucao.RootElement.GetProperty("valor").GetDecimal().ShouldBe(100m);
        devolucao.RootElement.GetProperty("moeda").GetString().ShouldBe("EUR");
    }

    [Fact]
    public async Task ConsultaDeTransferenciaDesconhecida_RespondeNaoLiquidada()
    {
        var id = Guid.CreateVersion7();

        await ConsultarAsync(id);

        using var payload = JsonDocument.Parse((await _repositorio.ListarRespostasAsync(id, Ct)).ShouldHaveSingleItem().Payload);
        payload.RootElement.GetProperty("status").GetString().ShouldBe("NaoLiquidada");
    }

    [Fact]
    public async Task Entrega_PublicaSoAsVencidasEUmaVezSo()
    {
        var id = Guid.CreateVersion7();
        await LiquidarAsync(id, "DE89370400440532013000");

        var antesDaHora = await EntregarAsync(id, Agora);
        var naHora = await EntregarAsync(id, Agora.AddSeconds(1));
        var depois = await EntregarAsync(id, Agora.AddSeconds(10));

        antesDaHora.ShouldBeEmpty();
        naHora.ShouldHaveSingleItem().Tipo.ShouldBe("LiquidacaoConfirmada");
        depois.ShouldBeEmpty();
    }

    [Fact]
    public async Task MesmaMensagemEntregueDuasVezes_EProcessadaUmaVezSo()
    {
        var id = Guid.CreateVersion7();
        var idMensagem = Guid.CreateVersion7().ToString();

        await LiquidarAsync(id, "DE89370400440532013000", idMensagem);
        await LiquidarAsync(id, "DE89370400440532013000", idMensagem);

        (await _repositorio.ListarRespostasAsync(id, Ct)).Count.ShouldBe(1);
    }
}
