using System.Net;
using EuroBank.Antifraude.Api.Tests.Fixtures;
using EuroBank.Antifraude.Grpc;
using static EuroBank.Antifraude.Api.Tests.Fixtures.CenarioAvaliacao;
using static EuroBank.Antifraude.Api.Tests.Fixtures.ClienteAnalista;

namespace EuroBank.Antifraude.Api.Tests.Endpoints;

/// <summary>Regras, limiares e listas mudam a próxima avaliação sem reiniciar (cada teste desfaz o que mudou).</summary>
[Collection(nameof(ColecaoConfiguracaoGlobal))]
public sealed class ConfiguracaoEndpointsTestes(AntifraudeFixture antifraude) : IDisposable
{
    private readonly ClienteAnalista _analista = new(antifraude);

    private readonly CenarioAvaliacao _cenario = new(antifraude);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Decisao> AvaliarAsync(decimal valor = 4_600m, string iban = IbanConhecido)
    {
        var conta = await _cenario.ContaComHistoricoAsync();
        return (await antifraude.CriarClienteGrpc().AvaliarAsync(Pedido(conta, valor, iban), cancellationToken: Ct)).Decisao;
    }

    [Fact]
    public async Task Regras_ListaAsNove_EConsultaPorCodigo()
    {
        (await _analista.GetAsync("/regras")).GetArrayLength().ShouldBe(9);

        var velocidade = await _analista.GetAsync("/regras/velocidade");
        velocidade.GetProperty("codigo").GetString().ShouldBe("Velocidade");
        velocidade.GetProperty("parametros").GetProperty("quantidade").GetDecimal().ShouldBe(3m);

        await DeveSerProblemaAsync(await _analista.Http.GetAsync("/regras/inexistente", Ct), HttpStatusCode.NotFound, "regra_nao_encontrada");

        // O sinal do modelo tem código de regra, mas não é uma regra da tabela.
        await DeveSerProblemaAsync(await _analista.Http.GetAsync("/regras/ModeloFraude", Ct), HttpStatusCode.NotFound, "regra_nao_encontrada");
    }

    [Fact]
    public async Task NovoPeso_MudaAProximaAvaliacao()
    {
        (await AvaliarAsync()).ShouldBe(Decisao.EmRevisao);

        await OkAsync(await _analista.PutAsync("/regras/ValorAcimaDaMedia", new { peso = 50, ativa = true, sombra = false }));
        try
        {
            (await AvaliarAsync()).ShouldBe(Decisao.Recusada);
        }
        finally
        {
            await OkAsync(await _analista.PutAsync("/regras/ValorAcimaDaMedia", new { peso = 25, ativa = true, sombra = false }));
        }
    }

    [Fact]
    public async Task RegraEmSombra_NaoPontua()
    {
        await OkAsync(await _analista.PutAsync("/regras/Esvaziamento", new { peso = 25, ativa = true, sombra = true }));
        try
        {
            (await AvaliarAsync()).ShouldBe(Decisao.Aprovada);
        }
        finally
        {
            await OkAsync(await _analista.PutAsync("/regras/Esvaziamento", new { peso = 25, ativa = true, sombra = false }));
        }
    }

    [Fact]
    public async Task AjusteInvalido_400()
    {
        await DeveSerProblemaAsync(
            await _analista.PutAsync("/regras/Velocidade", new { peso = 20, ativa = true, sombra = false, parametros = new { multiplicador = 2 } }),
            HttpStatusCode.BadRequest,
            "parametro_regra_invalido");
        await DeveSerProblemaAsync(
            await _analista.PutAsync("/regras/Velocidade", new { peso = 120, ativa = true, sombra = false }),
            HttpStatusCode.BadRequest,
            "parametro_regra_invalido");
    }

    [Fact]
    public async Task NovosLimiares_MudamAProximaAvaliacao()
    {
        var atual = await _analista.GetAsync("/configuracao-decisao");
        var nova = await OkAsync(await _analista.PutAsync("/configuracao-decisao", new { limiarRevisao = 60, limiarRecusa = 80, pesoModelo = 0 }));
        nova.GetProperty("versao").GetInt32().ShouldBe(atual.GetProperty("versao").GetInt32() + 1);
        try
        {
            (await AvaliarAsync()).ShouldBe(Decisao.Aprovada);
        }
        finally
        {
            await OkAsync(await _analista.PutAsync("/configuracao-decisao", new { limiarRevisao = 30, limiarRecusa = 70, pesoModelo = 50 }));
        }
    }

    [Fact]
    public async Task LimiaresInvalidos_400()
    {
        await DeveSerProblemaAsync(
            await _analista.PutAsync("/configuracao-decisao", new { limiarRevisao = 70, limiarRecusa = 30, pesoModelo = 0 }),
            HttpStatusCode.BadRequest,
            "configuracao_invalida");
    }

    [Fact]
    public async Task IbanBloqueadoEDesbloqueado_MudaAProximaAvaliacao()
    {
        var iban = IbanUnico();
        (await AvaliarAsync(100m, iban)).ShouldBe(Decisao.Aprovada);

        var bloqueio = await OkAsync(await _analista.PostAsync("/listas/bloqueio", new { iban, motivo = "Mula", analista = "Marta" }));
        bloqueio.GetProperty("iban").GetString().ShouldBe(iban);
        (await AvaliarAsync(100m, iban)).ShouldBe(Decisao.Recusada);

        // Bloquear de novo devolve o bloqueio que já existe.
        (await OkAsync(await _analista.PostAsync("/listas/bloqueio", new { iban, motivo = "Outro", analista = "Paulo" })))
            .GetProperty("bloqueadoPor").GetString().ShouldBe("Marta");

        (await _analista.DeleteAsync($"/listas/bloqueio/{iban}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AvaliarAsync(100m, iban)).ShouldBe(Decisao.Aprovada);
        await DeveSerProblemaAsync(await _analista.DeleteAsync($"/listas/bloqueio/{iban}"), HttpStatusCode.NotFound, "iban_nao_bloqueado");
    }

    [Fact]
    public async Task PaisDeRiscoIncluidoERemovido_MudaAProximaAvaliacao()
    {
        // Malta: beneficiário novo (10) + país de risco (30) = 40, revisão; sem o país, 10 aprova.
        const string ibanMalta = "MT84MALT011000012345MTLCAST001S";
        (await AvaliarAsync(100m, ibanMalta)).ShouldBe(Decisao.Aprovada);

        (await OkAsync(await _analista.PostAsync("/listas/paises-risco", new { codigo = "mt" }))).GetProperty("codigo").GetString().ShouldBe("MT");
        try
        {
            (await AvaliarAsync(100m, ibanMalta)).ShouldBe(Decisao.EmRevisao);
        }
        finally
        {
            (await _analista.DeleteAsync("/listas/paises-risco/MT")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        (await AvaliarAsync(100m, ibanMalta)).ShouldBe(Decisao.Aprovada);
        (await _analista.GetAsync("/listas/paises-risco")).EnumerateArray().Select(pais => pais.GetProperty("codigo").GetString())
            .ShouldBe(["IR", "KP", "MM"]);
        await DeveSerProblemaAsync(await _analista.PostAsync("/listas/paises-risco", new { codigo = "MTA" }), HttpStatusCode.BadRequest, "pais_invalido");
        await DeveSerProblemaAsync(await _analista.DeleteAsync("/listas/paises-risco/MT"), HttpStatusCode.NotFound, "pais_nao_listado");
    }

    public void Dispose() => _analista.Dispose();
}
