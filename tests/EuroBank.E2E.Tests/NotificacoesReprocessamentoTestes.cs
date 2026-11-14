using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.E2E.Tests.Fixtures;
using Npgsql;

namespace EuroBank.E2E.Tests;

/// <summary>
/// Reprocessar eurobank.transferencias.eventos não duplica e-mails, nem quando a inbox do Wolverine já esqueceu as
/// mensagens (o que acontece depois de 7 dias): a tabela de notificações tem o id do evento como chave.
/// </summary>
[Collection(nameof(ColecaoSemParalelismo))]
public sealed class NotificacoesReprocessamentoTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(90);

    private readonly CenarioSaga _cenario = new(lab);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OutroConsumerGroupSemInbox_LeOTopicoDeNovo_ENaoReenvia()
    {
        var email = $"reprocessa-{Guid.NewGuid():N}@exemplo.com";
        var conta = await _cenario.AbrirContaAsync(1_000m);
        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m, emailNotificacao: email);
        await _cenario.EsperarEstadoAsync(pedido.Id, "Concluida", Limite);
        await Esperar.AteAsync(async () => (await QuantosEmailsAsync(email)) == 1, Limite, "o primeiro e-mail não chegou");

        // A inbox esquece os ids (como depois da janela de 7 dias): só a tabela de notificações segura a repetição.
        await ExecutarAsync("DELETE FROM wolverine.wolverine_incoming_envelopes");

        var grupo = $"eurobank-notificacoes-reprocessa-{Guid.NewGuid():N}";
        await using (lab.SubirOutroWorkerDeNotificacoes(grupo))
        {
            await lab.EsperarGrupoNoFimDoTopicoAsync(grupo, "eurobank.transferencias.eventos", Limite);
        }

        (await QuantosEmailsAsync(email)).ShouldBe(1);
        (await TentativasAsync(pedido.Id)).ShouldBe(1);
    }

    private async Task<int> QuantosEmailsAsync(string destinatario)
    {
        var busca = await lab.Mailpit.GetFromJsonAsync<JsonElement>(
            $"/api/v1/search?query={Uri.EscapeDataString($"to:{destinatario}")}", Ct);
        return busca.GetProperty("messages").GetArrayLength();
    }

    private async Task<int> TentativasAsync(Guid transferenciaId)
    {
        await using var conexao = new NpgsqlConnection(lab.ConnectionStringNotificacoes);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand(
            "SELECT tentativas FROM notificacoes WHERE transferencia_id = @id AND status = 'Enviada'", conexao);
        comando.Parameters.AddWithValue("id", transferenciaId);
        return (int)(await comando.ExecuteScalarAsync(Ct))!;
    }

    private async Task ExecutarAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(lab.ConnectionStringNotificacoes);
        await conexao.OpenAsync(Ct);
        await using var comando = new NpgsqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync(Ct);
    }
}
