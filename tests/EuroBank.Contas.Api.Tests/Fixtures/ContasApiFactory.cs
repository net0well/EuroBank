using EuroBank.Contas.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>Sobe a API de Contas in-process (ambiente Development: migrations e seed no startup).</summary>
public sealed class ContasApiFactory(string connectionString) : WebApplicationFactory<MarcadorContasApi>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Contas", connectionString);
        builder.UseSetting("OTEL_SDK_DISABLED", "true");
    }
}
