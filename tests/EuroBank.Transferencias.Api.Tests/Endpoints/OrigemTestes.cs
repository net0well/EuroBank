using System.Net;
using System.Net.Http.Json;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>
/// Os headers X-Dispositivo-Id e X-Localizacao ficam na transferência (a varredura os manda ao antifraude); fora do
/// formato, 400; ausentes, a transferência segue sem origem.
/// </summary>
public sealed class OrigemTestes(PostgresFixture banco)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpResponseMessage> SolicitarAsync(string? dispositivo, string? localizacao)
    {
        var conta = await banco.NovaContaAsync();
        using var cliente = banco.Api.CriarClienteAutenticado();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/transferencias-externas") { Content = JsonContent.Create(Pedido(conta.Id)) };
        requisicao.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString());
        if (dispositivo is not null)
        {
            requisicao.Headers.Add("X-Dispositivo-Id", dispositivo);
        }

        if (localizacao is not null)
        {
            requisicao.Headers.Add("X-Localizacao", localizacao);
        }

        return await cliente.SendAsync(requisicao, Ct);
    }

    private async Task<(string? Dispositivo, Localizacao? Localizacao)> OrigemGravadaAsync(Guid id)
    {
        await using var contexto = banco.CriarContexto();
        var transferencia = await contexto.Transferencias.AsNoTracking().SingleAsync(registro => registro.Id == new TransferenciaId(id), Ct);
        return (transferencia.DispositivoId, transferencia.Localizacao);
    }

    [Fact]
    public async Task ComOsHeaders_AOrigemFicaNaTransferencia()
    {
        var transferencia = await LerAsync(await SolicitarAsync("celular-da-ana", "48.8566,2.3522,fr"), HttpStatusCode.Accepted);

        (await OrigemGravadaAsync(transferencia.Id)).ShouldBe(("celular-da-ana", new Localizacao(48.8566, 2.3522, "FR")));
    }

    [Fact]
    public async Task SemOsHeaders_SemOrigem()
    {
        var transferencia = await LerAsync(await SolicitarAsync(null, null), HttpStatusCode.Accepted);

        (await OrigemGravadaAsync(transferencia.Id)).ShouldBe((null, null));
    }

    [Theory]
    [InlineData("48.8566;2.3522;FR")]
    [InlineData("91,0,FR")]
    [InlineData("0,0,França")]
    public async Task LocalizacaoForaDoFormato_400(string localizacao)
    {
        await DeveSerProblemaAsync(await SolicitarAsync("celular", localizacao), HttpStatusCode.BadRequest, "localizacao_invalida");
    }

    [Fact]
    public async Task DispositivoLongoDemais_400()
    {
        await DeveSerProblemaAsync(await SolicitarAsync(new string('x', 101), null), HttpStatusCode.BadRequest, "dispositivo_invalido");
    }
}
