using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.E2E.Tests.Fixtures;
using Npgsql;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Do fim da saga ao e-mail no Mailpit: Transferencias publica o evento final pela outbox, o Debezium leva ao Kafka
/// e o worker de notificações manda o e-mail em pt-BR.
/// </summary>
public sealed class NotificacoesTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Concluida_ChegaUmEmailNoMailpit()
    {
        var email = EmailUnico();
        var conta = await _cenario.AbrirContaAsync(1_000m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, emailNotificacao: email);
        await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);

        var mensagem = (await EsperarEmailsAsync(email, 1)).ShouldHaveSingleItem();
        mensagem.GetProperty("Subject").GetString().ShouldBe("Transferência concluída: 100,00 EUR para Beneficiário 00");
        var completa = await lab.Mailpit.GetFromJsonAsync<JsonElement>($"/api/v1/message/{mensagem.GetProperty("ID").GetString()}", Ct);
        completa.GetProperty("Text").GetString()!.ShouldContain("Debitado da conta: 102,50 EUR (100,00 EUR + tarifa de 2,50 EUR)");
        completa.GetProperty("HTML").GetString()!.ShouldContain($"Transferência</td><td style=\"border-bottom:1px solid #e4e7eb\">{pedido.Id}");
    }

    [Fact]
    public async Task Recusada_EmailExplicaOMotivo()
    {
        var email = EmailUnico();
        var conta = await _cenario.AbrirContaAsync(50m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, emailNotificacao: email);
        await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        var mensagem = (await EsperarEmailsAsync(email, 1)).ShouldHaveSingleItem();
        mensagem.GetProperty("Subject").GetString().ShouldBe("Transferência não realizada: 100,00 EUR para Beneficiário 00");
        mensagem.GetProperty("Snippet").GetString()!.ShouldContain("saldo insuficiente");
    }

    [Fact]
    public async Task SemEmailNotificacao_NenhumEmail()
    {
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var comEmail = EmailUnico();

        var semEmail = await _cenario.SolicitarComSucessoAsync(conta, "00", 10m);
        await _cenario.EsperarEstadoAsync(semEmail.Id, "Concluida", Limite);

        // A seguinte da mesma conta cai na mesma partição, depois da primeira: quando o e-mail dela chega, o evento
        // da transferência sem e-mail já foi consumido.
        var depois = await _cenario.SolicitarComSucessoAsync(conta, "00", 10m, emailNotificacao: comEmail);
        await _cenario.EsperarEstadoAsync(depois.Id, "Concluida", Limite);
        await EsperarEmailsAsync(comEmail, 1);

        (await BuscarAsync(semEmail.Id.ToString())).ShouldBeEmpty();
        (await NotificacoesDaTransferenciaAsync(semEmail.Id)).ShouldBe(0);
    }

    private async Task<List<JsonElement>> EsperarEmailsAsync(string destinatario, int quantidade)
    {
        List<JsonElement> mensagens = [];
        await Esperar.AteAsync(
            async () => (mensagens = await BuscarAsync($"to:{destinatario}")).Count >= quantidade,
            Limite,
            $"o e-mail para {destinatario} não chegou ao Mailpit");
        return mensagens;
    }

    private async Task<List<JsonElement>> BuscarAsync(string consulta)
    {
        var busca = await lab.Mailpit.GetFromJsonAsync<JsonElement>($"/api/v1/search?query={Uri.EscapeDataString(consulta)}", Ct);
        return busca.GetProperty("messages").EnumerateArray().ToList();
    }

    private async Task<long> NotificacoesDaTransferenciaAsync(Guid transferenciaId)
    {
        await using var conexao = new NpgsqlConnection(lab.ConnectionStringNotificacoes);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand("SELECT count(*) FROM notificacoes WHERE transferencia_id = @id", conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        return (long)(await comando.ExecuteScalarAsync(Ct))!;
    }

    private static string EmailUnico() => $"cliente-{Guid.NewGuid():N}@exemplo.com";
}
