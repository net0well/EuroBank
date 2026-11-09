using EuroBank.Contas.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Sobe a API de Contas in-process (ambiente Development: migrations e seed no startup).</summary>
public sealed class ContasApiFactory(string connectionString) : WebApplicationFactory<MarcadorContasApi>
{
    public const string ApiKey = "chave-de-teste-contas";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Contas", connectionString);
        builder.UseSetting("Seguranca:ApiKey", ApiKey);
        builder.UseSetting("OTEL_SDK_DISABLED", "true");
    }

    /// <summary>Cliente HTTP que já envia o header X-Api-Key.</summary>
    public HttpClient CriarClienteAutenticado()
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return cliente;
    }
}
