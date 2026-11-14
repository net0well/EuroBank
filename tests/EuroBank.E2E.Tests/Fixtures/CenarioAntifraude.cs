using System.Net.Http.Json;
using System.Text.Json;

namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>Atalhos para o lado do analista: casos e análises pela REST e pelo JSON transcoding do antifraude.</summary>
public sealed class CenarioAntifraude(LaboratorioFixture lab)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task<JsonElement> CasoAsync(Guid casoId) =>
        await lab.Antifraude.GetFromJsonAsync<JsonElement>($"/casos/{casoId}", Ct);

    /// <summary>A análise pelo GET transcodificado do ObterAnalise.</summary>
    public async Task<JsonElement> AnaliseAsync(Guid analiseId) =>
        await lab.Antifraude.GetFromJsonAsync<JsonElement>($"/v1/avaliacoes/{analiseId}", Ct);

    /// <summary>Códigos das regras que pontuaram na análise (as em sombra ficam de fora).</summary>
    public async Task<List<string>> RegrasQuePontuaramAsync(Guid analiseId) =>
        (await AnaliseAsync(analiseId)).GetProperty("sinais").EnumerateArray()
            .Where(sinal => sinal.TryGetProperty("disparou", out var disparou) && disparou.GetBoolean()
                && !(sinal.TryGetProperty("sombra", out var sombra) && sombra.GetBoolean()))
            .Select(sinal => sinal.GetProperty("codigo").GetString()!)
            .ToList();

    /// <summary>A analista assume o caso e decide ("Aprovar" ou "Recusar"), com justificativa.</summary>
    public async Task DecidirAsync(Guid casoId, string decisao)
    {
        (await lab.Antifraude.PostAsJsonAsync($"/casos/{casoId}/atribuicao", new { analista = "Marta" }, Ct)).EnsureSuccessStatusCode();
        var corpo = new { analista = "Marta", decisao, justificativa = "Conferido com o cliente por telefone", bloquearIban = false };
        (await lab.Antifraude.PostAsJsonAsync($"/casos/{casoId}/decisao", corpo, Ct)).EnsureSuccessStatusCode();
    }

    /// <summary>A mesma operação avaliada de novo pelo POST transcodificado do Avaliar, como um segundo cliente faria.</summary>
    public async Task<JsonElement> AvaliarDeNovoAsync(Guid operacaoId, Guid contaId, string ibanDestino, decimal valor)
    {
        var pedido = new
        {
            operacaoId,
            tipoOperacao = "TIPO_OPERACAO_EXTERNA",
            contaId,
            valor = new { units = (long)valor, nanos = 0 },
            moeda = "EUR",
            ibanDestino,
        };
        var resposta = await lab.Antifraude.PostAsJsonAsync("/v1/avaliacoes", pedido, Ct);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    /// <summary>Quantos casos pendentes a fila tem para a operação.</summary>
    public async Task<int> CasosPendentesDaOperacaoAsync(Guid operacaoId) =>
        (await lab.Antifraude.GetFromJsonAsync<JsonElement>("/casos?tamanho=200", Ct)).GetProperty("itens").EnumerateArray()
            .Count(caso => caso.GetProperty("operacaoId").GetGuid() == operacaoId);
}
