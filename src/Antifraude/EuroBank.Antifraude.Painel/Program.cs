using EuroBank.Antifraude.Painel.Clients;
using EuroBank.Antifraude.Painel.Components;
using EuroBank.Antifraude.Painel.Messaging;
using EuroBank.Antifraude.Painel.Options;
using EuroBank.Antifraude.Painel.Services;
using EuroBank.Shared.Web.Extensions;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Wolverine;

// Painel do analista: Blazor Interactive Server sobre a REST do antifraude (a API key fica aqui, no servidor) e a fila
// em tempo real pelos eventos de eurobank.antifraude.eventos, entregues às telas pelo circuito.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-antifraude-painel");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddOptions<OpcoesAntifraude>().BindConfiguration(OpcoesAntifraude.Secao);
builder.Services.AddHttpClient<ClienteAntifraude>((provedor, http) =>
{
    var antifraude = provedor.GetRequiredService<IOptions<OpcoesAntifraude>>().Value;
    http.BaseAddress = antifraude.Endereco;
    http.DefaultRequestHeaders.Add("X-Api-Key", antifraude.ApiKey);
});
builder.Services.AddSingleton<AvisosFila>();
builder.Services.AddScoped<SessaoAnalista>();
builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaPainel(builder.Configuration));

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
