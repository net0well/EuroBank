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

    public async Task<Guid> AbrirContaAsync(decimal saldo) => (await AbrirContaEmAsync("EUR", saldo)).Id;

    /// <summary>Abre uma conta na moeda pedida e devolve o id e o IBAN dela.</summary>
    public async Task<(Guid Id, string Iban)> AbrirContaEmAsync(string moeda, decimal saldo = 0m)
    {
        var resposta = await lab.Contas.PostAsJsonAsync("/contas", new { titular = $"Saga E2E {moeda}", moeda }, Ct);
        resposta.EnsureSuccessStatusCode();
        var conta = await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var id = conta.GetProperty("id").GetGuid();
        if (saldo > 0)
        {
            (await lab.Contas.PostAsJsonAsync($"/contas/{id}/depositos", new { valor = saldo }, Ct)).EnsureSuccessStatusCode();
        }

        return (id, conta.GetProperty("iban").GetString()!);
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
        string? idempotencyKey = null,
        string? moedaDestino = null,
        string? moedaDoValor = null,
        string? emailNotificacao = null) =>
        EnviarAsync(
            "/transferencias-externas",
            new
            {
                contaOrigemId = contaId,
                ibanDestino = Iban(finalIban),
                bic = "DEUTDEFF",
                nomeBeneficiario = $"Beneficiário {finalIban}",
                valor,
                executarEm,
                moedaDestino,
                moedaDoValor,
                emailNotificacao,
            },
            idempotencyKey ?? Guid.CreateVersion7().ToString());

    public async Task<TransferenciaE2E> SolicitarComSucessoAsync(
        Guid contaId,
        string finalIban,
        decimal valor,
        DateTimeOffset? executarEm = null,
        string? moedaDestino = null,
        string? moedaDoValor = null,
        string? emailNotificacao = null) =>
        await LerAsync(
            await SolicitarAsync(
                contaId,
                finalIban,
                valor,
                executarEm,
                moedaDestino: moedaDestino,
                moedaDoValor: moedaDoValor,
                emailNotificacao: emailNotificacao),
            HttpStatusCode.Accepted);

    public async Task<TransferenciaE2E> SolicitarCambioComSucessoAsync(Guid contaId, string ibanDestino, decimal valor, string? moedaDoValor) =>
        await LerAsync(
            await EnviarAsync(
                "/transferencias-cambio",
                new { contaOrigemId = contaId, ibanDestino, valor, moedaDoValor },
                Guid.CreateVersion7().ToString()),
            HttpStatusCode.Accepted);

    /// <summary>
    /// Envia o pedido; se Transferencias ainda não conhece a conta (o ContaAberta ainda está a caminho da projeção),
    /// espera e repete com a mesma Idempotency-Key, como um cliente faria.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarAsync(string rota, object corpo, string idempotencyKey)
    {
        var fim = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var requisicao = new HttpRequestMessage(HttpMethod.Post, rota) { Content = JsonContent.Create(corpo) };
            requisicao.Headers.Add("Idempotency-Key", idempotencyKey);
            var resposta = await lab.Transferencias.SendAsync(requisicao, Ct);
            if (DateTime.UtcNow > fim || !await ContaAindaNaoProjetadaAsync(resposta))
            {
                return resposta;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300), Ct);
        }
    }

    private static async Task<bool> ContaAindaNaoProjetadaAsync(HttpResponseMessage resposta)
    {
        if (resposta.StatusCode != HttpStatusCode.UnprocessableEntity)
        {
            return false;
        }

        var corpo = await resposta.Content.ReadAsStringAsync(Ct);
        return corpo.Contains("conta_origem_desconhecida", StringComparison.Ordinal)
            || corpo.Contains("conta_destino_desconhecida", StringComparison.Ordinal);
    }

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
    decimal? ValorDebitado,
    decimal? ValorDestino,
    string MoedaDestino,
    CotacaoE2E? Cotacao,
    IReadOnlyList<PassoE2E> Historico)
{
    public bool PassouPor(string trecho) =>
        Historico.Any(passo => passo.Descricao.Contains(trecho, StringComparison.Ordinal));
}

public sealed record CotacaoE2E(Guid Id, decimal Taxa, DateTimeOffset ValidaAte);

public sealed record PassoE2E(int Ordem, string EstadoApos, string Descricao);
