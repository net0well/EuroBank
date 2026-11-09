using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>Atalhos para os testes da saga: contas pela API de Contas, transferências pela de Transferências.</summary>
public sealed class CenarioSaga(LaboratorioFixture lab)
{
    /// <summary>IBAN externo cujo final (dois últimos dígitos) comanda o banco externo simulado.</summary>
    public static string Iban(string final) => $"DE893704004405320130{final}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task<Guid> AbrirContaAsync(decimal saldo)
    {
        var resposta = await lab.Contas.PostAsJsonAsync("/contas", new { titular = "Saga E2E" }, Ct);
        resposta.EnsureSuccessStatusCode();
        var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetGuid();
        if (saldo > 0)
        {
            (await lab.Contas.PostAsJsonAsync($"/contas/{id}/depositos", new { valor = saldo }, Ct)).EnsureSuccessStatusCode();
        }

        return id;
    }

    public async Task<decimal> SaldoAsync(Guid contaId) =>
        (await lab.Contas.GetFromJsonAsync<JsonElement>($"/contas/{contaId}", Ct)).GetProperty("saldo").GetDecimal();

    public Task<HttpResponseMessage> ComandoContaAsync(Guid contaId, string recurso) =>
        lab.Contas.PostAsync($"/contas/{contaId}/{recurso}", content: null, Ct);

    public Task<HttpResponseMessage> SolicitarAsync(
        Guid contaId,
        string finalIban,
        decimal valor,
        DateTimeOffset? executarEm = null,
        string? idempotencyKey = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/transferencias-externas")
        {
            Content = JsonContent.Create(new
            {
                contaOrigemId = contaId,
                ibanDestino = Iban(finalIban),
                bic = "DEUTDEFF",
                nomeBeneficiario = $"Beneficiário {finalIban}",
                valor,
                executarEm,
            }),
        };
        requisicao.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.CreateVersion7().ToString());
        return lab.Transferencias.SendAsync(requisicao, Ct);
    }

    public async Task<TransferenciaE2E> SolicitarComSucessoAsync(
        Guid contaId,
        string finalIban,
        decimal valor,
        DateTimeOffset? executarEm = null) =>
        await LerAsync(await SolicitarAsync(contaId, finalIban, valor, executarEm), HttpStatusCode.Accepted);

    public Task<HttpResponseMessage> CancelarAsync(Guid id) =>
        lab.Transferencias.PostAsync($"/transferencias/{id}/cancelamento", content: null, Ct);

    public async Task<TransferenciaE2E> ObterAsync(Guid id) =>
        (await lab.Transferencias.GetFromJsonAsync<TransferenciaE2E>($"/transferencias/{id}", Ct))!;

    /// <summary>Espera a saga chegar ao estado e devolve a transferência nesse ponto.</summary>
    public async Task<TransferenciaE2E> EsperarEstadoAsync(Guid id, string estado, TimeSpan limite)
    {
        TransferenciaE2E? atual = null;
        await Esperar.AteAsync(async () => (atual = await ObterAsync(id)).Estado == estado, limite,
            $"a transferência {id} não chegou a {estado} (último estado: {atual?.Estado})");
        return atual!;
    }

    /// <summary>Tipos dos lançamentos no extrato, do mais antigo para o mais recente, quando o extrato alcança a versão.</summary>
    public async Task<List<JsonElement>> EsperarLancamentosAsync(Guid contaId, int quantidade, TimeSpan limite)
    {
        List<JsonElement> lancamentos = [];
        await Esperar.AteAsync(async () =>
        {
            var resposta = await lab.Extrato.GetAsync($"/extratos/{contaId}", Ct);
            if (!resposta.IsSuccessStatusCode)
            {
                return false;
            }

            var extrato = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
            lancamentos = extrato.GetProperty("lancamentos").EnumerateArray().Reverse().ToList();
            return lancamentos.Count >= quantidade;
        }, limite, $"o extrato da conta {contaId} não chegou a {quantidade} lançamentos");
        return lancamentos;
    }

    public static async Task<TransferenciaE2E> LerAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        resposta.StatusCode.ShouldBe(esperado, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<TransferenciaE2E>(Ct))!;
    }

    public static async Task DeveSerProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status, string codigo)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        resposta.StatusCode.ShouldBe(status, corpo);
        using var problema = JsonDocument.Parse(corpo);
        problema.RootElement.GetProperty("codigo").GetString().ShouldBe(codigo);
    }
}

public sealed record TransferenciaE2E(
    Guid Id,
    string Estado,
    string? Motivo,
    bool CancelamentoSolicitado,
    decimal Valor,
    decimal Tarifa,
    IReadOnlyList<PassoE2E> Historico)
{
    public bool PassouPor(string trecho) =>
        Historico.Any(passo => passo.Descricao.Contains(trecho, StringComparison.Ordinal));
}

public sealed record PassoE2E(int Ordem, string EstadoApos, string Descricao);
