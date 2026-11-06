using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EuroBank.E2E.Tests.Fixtures;
using Npgsql;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Fluxo completo com Debezium de verdade: operação na API de Contas, linha na outbox, WAL, Kafka, Extrato.
/// </summary>
public sealed class FluxoCdcTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Deposito_ChegaAoKafkaNosDoisFormatos_AtualizaOExtrato_ENumTraceSo()
    {
        var conta = await AbrirContaAsync("E2E deposito");

        (await lab.Contas.PostAsJsonAsync($"/contas/{conta.Id}/depositos", new { valor = 100m }, Ct)).EnsureSuccessStatusCode();

        // Outbox: chave = id da conta, headers id/tipo/traceparent, corpo = JSON do evento (objeto, não string).
        var outbox = LeitorTopico.Encontrar(lab.BootstrapKafka, LaboratorioFixture.TopicoOutbox,
            m => m.Message.Key == conta.Id.ToString() && m.Header("tipo") == "DepositoRealizado", Limite);
        Guid.TryParse(outbox.Header("id"), out _).ShouldBeTrue();
        var traceparent = outbox.Header("traceparent");
        traceparent.ShouldNotBeNull();
        traceparent.ShouldStartWith("00-");
        using (var corpo = JsonDocument.Parse(outbox.Message.Value))
        {
            corpo.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
            corpo.RootElement.GetProperty("valor").GetString().ShouldBe("100.00");
            corpo.RootElement.GetProperty("versao").GetInt32().ShouldBe(2);
        }

        // CDC bruto: o UPDATE da linha de contas, com before e after legíveis.
        var cdc = LeitorTopico.Encontrar(lab.BootstrapKafka, LaboratorioFixture.TopicoCdc, m =>
        {
            using var mudanca = JsonDocument.Parse(m.Message.Value);
            return mudanca.RootElement.GetProperty("op").GetString() == "u"
                && mudanca.RootElement.GetProperty("after").GetProperty("id").GetString() == conta.Id.ToString()
                && mudanca.RootElement.GetProperty("after").GetProperty("versao").GetInt32() == 2;
        }, Limite);
        using (var mudanca = JsonDocument.Parse(cdc.Message.Value))
        {
            mudanca.RootElement.GetProperty("before").GetProperty("saldo").GetString().ShouldBe("0.00");
            mudanca.RootElement.GetProperty("after").GetProperty("saldo").GetString().ShouldBe("100.00");
        }

        // Extrato converge.
        await EsperarExtratoAsync(conta.Id, saldo: 100m, versao: 2);

        // Trace: o processamento no Extrato continua o trace iniciado pela requisição em Contas.
        var traceId = traceparent.Split('-')[1];
        await Esperar.AteAsync(() => Task.FromResult(lab.Atividades.Any(a =>
                a.TraceId.ToHexString() == traceId && a.DisplayName.Contains("EventoDepositoRealizado", StringComparison.Ordinal))),
            Limite, "a Activity do Extrato não está no trace da requisição de Contas");
    }

    [Fact]
    public async Task ConnectParado_OsEventosNaoSePerdem_EChegamQuandoEleVolta()
    {
        var conta = await AbrirContaAsync("E2E connect parado");
        await EsperarExtratoAsync(conta.Id, saldo: 0m, versao: 1);

        await lab.PararConnectAsync();
        try
        {
            for (var i = 0; i < 3; i++)
            {
                (await lab.Contas.PostAsJsonAsync($"/contas/{conta.Id}/depositos", new { valor = 10m }, Ct))
                    .EnsureSuccessStatusCode();
            }
        }
        finally
        {
            await lab.IniciarConnectAsync();
        }

        await EsperarExtratoAsync(conta.Id, saldo: 30m, versao: 4);
    }

    [Fact]
    public async Task MesmoEventoEntregueDuasVezes_EAplicadoUmaVezSo()
    {
        var conta = await AbrirContaAsync("E2E duplicata");
        (await lab.Contas.PostAsJsonAsync($"/contas/{conta.Id}/depositos", new { valor = 50m }, Ct)).EnsureSuccessStatusCode();
        await EsperarExtratoAsync(conta.Id, saldo: 50m, versao: 2);

        // Republica exatamente a mesma mensagem (mesmo header id), como aconteceria numa reentrega.
        var original = LeitorTopico.Encontrar(lab.BootstrapKafka, LaboratorioFixture.TopicoOutbox,
            m => m.Message.Key == conta.Id.ToString() && m.Header("tipo") == "DepositoRealizado", Limite);
        await ProduzirAsync(original.Message);
        var eventoId = Guid.Parse(original.Header("id")!);

        // A inbox durável registra o id uma vez só, e o extrato não muda.
        await Task.Delay(TimeSpan.FromSeconds(5), Ct);
        (await ContarNaInboxAsync(eventoId)).ShouldBe(1);
        var extrato = await ObterExtratoAsync(conta.Id);
        extrato.GetProperty("saldoAtual").GetDecimal().ShouldBe(50m);
        extrato.GetProperty("total").GetInt64().ShouldBe(1);
    }

    [Fact]
    public async Task MensagemInvalida_VaiParaODeadLetter_EOConsumoContinua()
    {
        var conta = await AbrirContaAsync("E2E dead letter");
        await EsperarExtratoAsync(conta.Id, saldo: 0m, versao: 1);
        var deadLettersAntes = await ContarDeadLettersAsync();

        // Na mesma chave da conta: sem header tipo, e com tipo conhecido mas corpo que não é JSON.
        await ProduzirAsync(new Message<string, string> { Key = conta.Id.ToString(), Value = """{"qualquer":"coisa"}""" });
        await ProduzirAsync(new Message<string, string>
        {
            Key = conta.Id.ToString(),
            Value = "isto não é json",
            Headers = new Headers { { "tipo", Encoding.UTF8.GetBytes("DepositoRealizado") }, { "id", Encoding.UTF8.GetBytes(Guid.CreateVersion7().ToString()) } },
        });

        await Esperar.AteAsync(async () => await ContarDeadLettersAsync() >= deadLettersAntes + 2, Limite,
            "as duas mensagens inválidas não foram para o dead letter");

        // As mensagens seguintes da mesma conta continuam sendo processadas.
        (await lab.Contas.PostAsJsonAsync($"/contas/{conta.Id}/depositos", new { valor = 7m }, Ct)).EnsureSuccessStatusCode();
        await EsperarExtratoAsync(conta.Id, saldo: 7m, versao: 2);
    }

    private async Task<ContaCriada> AbrirContaAsync(string titular)
    {
        var resposta = await lab.Contas.PostAsJsonAsync("/contas", new { titular }, Ct);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ContaCriada>(Ct))!;
    }

    private async Task<JsonElement> ObterExtratoAsync(Guid contaId)
    {
        using var documento = JsonDocument.Parse(await lab.Extrato.GetStringAsync($"/extratos/{contaId}", Ct));
        return documento.RootElement.Clone();
    }

    private Task EsperarExtratoAsync(Guid contaId, decimal saldo, int versao) =>
        Esperar.AteAsync(async () =>
        {
            var resposta = await lab.Extrato.GetAsync($"/extratos/{contaId}", Ct);
            if (!resposta.IsSuccessStatusCode)
            {
                return false;
            }

            var extrato = await ObterExtratoAsync(contaId);
            return extrato.GetProperty("saldoAtual").GetDecimal() == saldo
                && extrato.GetProperty("ultimaVersao").GetInt32() == versao;
        }, Limite, $"o extrato da conta {contaId} não chegou a saldo {saldo} na versão {versao}");

    private async Task ProduzirAsync(Message<string, string> mensagem)
    {
        using var produtor = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = lab.BootstrapKafka }).Build();
        await produtor.ProduceAsync(LaboratorioFixture.TopicoOutbox, mensagem, Ct);
    }

    private async Task<long> ContarNaInboxAsync(Guid eventoId)
    {
        await using var conexao = new NpgsqlConnection(lab.ConnectionStringExtrato);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand(
            "SELECT count(*) FROM wolverine.wolverine_incoming_envelopes WHERE id = @id", conexao);
        comando.Parameters.AddWithValue("id", eventoId);
        return (long)(await comando.ExecuteScalarAsync(Ct))!;
    }

    private async Task<long> ContarDeadLettersAsync()
    {
        await using var conexao = new NpgsqlConnection(lab.ConnectionStringExtrato);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM wolverine.wolverine_dead_letters", conexao);
        return (long)(await comando.ExecuteScalarAsync(Ct))!;
    }

    private sealed record ContaCriada(Guid Id, string Iban);
}
