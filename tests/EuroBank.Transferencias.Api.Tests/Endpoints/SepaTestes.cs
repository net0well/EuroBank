using System.Net;
using EuroBank.Transferencias.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using static EuroBank.Transferencias.Api.Tests.Fixtures.ClienteTransferencias;

namespace EuroBank.Transferencias.Api.Tests.Endpoints;

/// <summary>Regras SEPA e SWIFT das transferências externas (horário de corte, país, BIC).</summary>
public sealed class SepaTestes(PostgresFixture banco)
{
    private readonly ClienteTransferencias _cliente = new(banco.Api.CriarClienteAutenticado());

    [Fact]
    public async Task EmEuroNaSextaAs17_FicaAgendadaParaSegundaAs8()
    {
        // 2026-11-13 é uma sexta-feira.
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 13, 17, 0, 0, TimeSpan.Zero));
        await using var api = banco.Api.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Sepa:AplicarHorarioDeCorte", "true");
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(relogio));
        });
        using var http = api.CreateClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", TransferenciasApiFactory.ApiKey);

        var transferencia = await new ClienteTransferencias(http).SolicitarComSucessoAsync(Pedido());

        transferencia.Estado.ShouldBe("Agendada");
        transferencia.ExecutarEmEfetivo.ShouldBe(new DateTimeOffset(2026, 11, 16, 8, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("US64SVBKUS6S3300958879", "SVBKUS6S", "pais_fora_da_sepa")]
    [InlineData(IbanComum, "DEUT", "bic_invalido")]
    public async Task EmEuro_PaisForaDaSepaOuBicInvalido_Responde422(string iban, string bic, string codigo)
    {
        await DeveSerProblemaAsync(
            await _cliente.SolicitarAsync(Pedido(iban: iban, bic: bic), Guid.CreateVersion7().ToString()),
            HttpStatusCode.UnprocessableEntity,
            codigo);
    }

    [Fact]
    public async Task EmDolarParaForaDaSepa_Aceita()
    {
        var transferencia = await _cliente.SolicitarComSucessoAsync(
            Pedido(iban: "US64SVBKUS6S3300958879", bic: "SVBKUS6S", moedaDestino: "USD"));

        transferencia.MoedaDestino.ShouldBe("USD");
    }
}
