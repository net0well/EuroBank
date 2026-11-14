using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Grpc;
using Grpc.Health.V1;
using Grpc.Reflection.V1Alpha;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;

namespace EuroBank.Antifraude.Api.Tests.Grpc;

/// <summary>JSON transcoding, reflection, health check gRPC e o Swagger das rotas transcodificadas.</summary>
public sealed class InfraestruturaGrpcTestes(AntifraudeFixture antifraude)
{
    private readonly CenarioAvaliacao _cenario = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object CorpoJson(Guid conta, Guid operacao) => new
    {
        operacaoId = operacao.ToString(),
        tipoOperacao = "TIPO_OPERACAO_EXTERNA",
        contaId = conta.ToString(),
        valor = new { units = 4600, nanos = 0 },
        moeda = "EUR",
        ibanDestino = IbanConhecido,
        momento = Agora.ToString("O"),
    };

    [Fact]
    public async Task Transcoding_MesmoResultadoQueOGrpc()
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        var operacao = Guid.CreateVersion7();
        using var http = antifraude.CriarClienteHttp();

        var resposta = await http.PostAsJsonAsync("/v1/avaliacoes", CorpoJson(conta, operacao), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync(Ct));
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
        json.GetProperty("decisao").GetString().ShouldBe("DECISAO_EM_REVISAO");
        json.GetProperty("scoreFinal").GetInt32().ShouldBe(50);

        var porGrpc = await antifraude.CriarClienteGrpc().AvaliarAsync(Pedido(conta, 4_600m, operacao: operacao), cancellationToken: Ct);
        porGrpc.AnaliseId.ShouldBe(json.GetProperty("analiseId").GetString());
        porGrpc.CasoId.ShouldBe(json.GetProperty("casoId").GetString());

        var consulta = await http.GetFromJsonAsync<JsonElement>($"/v1/avaliacoes/{porGrpc.AnaliseId}", Ct);
        consulta.GetProperty("analiseId").GetString().ShouldBe(porGrpc.AnaliseId);
    }

    [Fact]
    public async Task Transcoding_SemApiKey_401()
    {
        using var http = antifraude.CriarClienteHttp(comApiKey: false);

        var resposta = await http.PostAsJsonAsync("/v1/avaliacoes", CorpoJson(Guid.CreateVersion7(), Guid.CreateVersion7()), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reflection_ListaOsServicos_SemApiKey()
    {
        var reflection = new ServerReflection.ServerReflectionClient(antifraude.CriarCanal());
        using var chamada = reflection.ServerReflectionInfo(cancellationToken: Ct);

        await chamada.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = string.Empty }, Ct);
        await chamada.RequestStream.CompleteAsync();
        await chamada.ResponseStream.MoveNext(Ct);

        var servicos = chamada.ResponseStream.Current.ListServicesResponse.Service.Select(servico => servico.Name).ToList();
        servicos.ShouldContain("eurobank.antifraude.v1.AntifraudeService");
        servicos.ShouldContain("grpc.health.v1.Health");
    }

    [Fact]
    public async Task HealthGrpc_Serving_SemApiKey()
    {
        var health = new Health.HealthClient(antifraude.CriarCanal());

        var resposta = await health.CheckAsync(new HealthCheckRequest(), cancellationToken: Ct);

        resposta.Status.ShouldBe(HealthCheckResponse.Types.ServingStatus.Serving);
    }

    [Fact]
    public async Task Swagger_TemAsRotasTranscodificadas_ComOsCamposDoProto()
    {
        using var http = antifraude.CriarClienteHttp(comApiKey: false);

        var documento = await http.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json", Ct);

        var caminhos = documento.GetProperty("paths");
        caminhos.TryGetProperty("/v1/avaliacoes", out var avaliacoes).ShouldBeTrue();
        caminhos.TryGetProperty("/v1/avaliacoes/{analiseId}", out var consulta).ShouldBeTrue();
        caminhos.TryGetProperty("/v1/simulacoes", out _).ShouldBeTrue();
        consulta.TryGetProperty("get", out _).ShouldBeTrue();

        var corpo = avaliacoes.GetProperty("post").GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("properties");
        corpo.TryGetProperty("operacaoId", out _).ShouldBeTrue();
        corpo.TryGetProperty("ibanDestino", out _).ShouldBeTrue();
        corpo.GetProperty("valor").GetProperty("properties").TryGetProperty("units", out _).ShouldBeTrue();
        corpo.GetProperty("tipoOperacao").GetProperty("enum").EnumerateArray().Select(valor => valor.GetString())
            .ShouldContain("TIPO_OPERACAO_EXTERNA");
    }
}
