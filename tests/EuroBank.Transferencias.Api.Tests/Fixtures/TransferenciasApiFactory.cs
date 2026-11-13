using EuroBank.Transferencias.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EuroBank.Transferencias.Api.Tests.Fixtures;

/// <summary>Sobe a API de Transferências in-process (ambiente Development: migrations no startup).</summary>
public sealed class TransferenciasApiFactory(string connectionString) : WebApplicationFactory<MarcadorTransferenciasApi>
{
    public const string ApiKey = "chave-de-teste-transferencias";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Transferencias", connectionString);
        builder.UseSetting("Seguranca:ApiKey", ApiKey);
        builder.UseSetting("OTEL_SDK_DISABLED", "true");

        // Os testes não podem depender da hora em que rodam; o corte SEPA tem testes próprios com relógio falso.
        builder.UseSetting("Sepa:AplicarHorarioDeCorte", "false");
    }

    /// <summary>Cliente HTTP que já envia o header X-Api-Key.</summary>
    public HttpClient CriarClienteAutenticado()
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return cliente;
    }
}
