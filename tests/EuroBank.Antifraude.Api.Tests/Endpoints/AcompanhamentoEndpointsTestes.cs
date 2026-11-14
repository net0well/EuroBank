using System.Net;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using static EuroBank.Antifraude.Api.Tests.Fixtures.ClienteAnalista;

namespace EuroBank.Antifraude.Api.Tests.Endpoints;

/// <summary>Fraudes confirmadas (rótulos para o retreino) e a visão geral das últimas 24 horas.</summary>
public sealed class AcompanhamentoEndpointsTestes(AntifraudeFixture antifraude) : IDisposable
{
    private readonly ClienteAnalista _analista = new(antifraude);

    [Fact]
    public async Task FraudeConfirmada_DeOperacaoAnalisada_EIdempotente()
    {
        var (_, operacaoId) = await _analista.CasoPendenteAsync();

        var primeira = await OkAsync(await _analista.PostAsync(
            "/fraudes-confirmadas", new { operacaoId, analista = "Marta", observacao = "Cliente não reconhece" }));
        var segunda = await OkAsync(await _analista.PostAsync(
            "/fraudes-confirmadas", new { operacaoId, analista = "Paulo" }));

        primeira.GetProperty("operacaoId").GetGuid().ShouldBe(operacaoId);
        primeira.GetProperty("observacao").GetString().ShouldBe("Cliente não reconhece");
        segunda.GetProperty("id").GetGuid().ShouldBe(primeira.GetProperty("id").GetGuid());
        segunda.GetProperty("analista").GetString().ShouldBe("Marta");
    }

    [Fact]
    public async Task FraudeConfirmada_SemAnaliseOuSemAnalista()
    {
        await DeveSerProblemaAsync(
            await _analista.PostAsync("/fraudes-confirmadas", new { operacaoId = Guid.CreateVersion7(), analista = "Marta" }),
            HttpStatusCode.NotFound,
            "operacao_nao_analisada");

        var (_, operacaoId) = await _analista.CasoPendenteAsync();
        await DeveSerProblemaAsync(
            await _analista.PostAsync("/fraudes-confirmadas", new { operacaoId, analista = " " }),
            HttpStatusCode.BadRequest,
            "analista_obrigatorio");
    }

    [Fact]
    public async Task VisaoGeral_ContaAnalisesCasosERegras()
    {
        await _analista.CasoPendenteAsync();

        var visao = await _analista.GetAsync("/visao-geral");

        visao.GetProperty("analises").GetInt32().ShouldBeGreaterThan(0);
        visao.GetProperty("porDecisao").TryGetProperty("EmRevisao", out _).ShouldBeTrue();
        visao.GetProperty("casosPendentes").GetInt32().ShouldBeGreaterThan(0);
        visao.GetProperty("taxaRevisao").GetDecimal().ShouldBeGreaterThan(0m);
        visao.GetProperty("regrasMaisDisparadas").EnumerateArray()
            .Select(regra => regra.GetProperty("codigo").GetString())
            .ShouldContain("Esvaziamento");
    }

    public void Dispose() => _analista.Dispose();
}
