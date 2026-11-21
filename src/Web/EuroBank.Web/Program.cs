using EuroBank.Shared.Web.Extensions;
using EuroBank.Web.Antifraude.Clients;
using EuroBank.Web.Antifraude.Messaging;
using EuroBank.Web.Antifraude.Options;
using EuroBank.Web.Antifraude.Services;
using EuroBank.Web.Components;
using EuroBank.Web.Options;
using EuroBank.Web.Services;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Wolverine;

// EuroBank.Web: as funcionalidades web do laboratório em Blazor Interactive Server, uma área por assunto. No antifraude,
// a REST do antifraude pelo servidor (a API key fica aqui) e a fila em tempo real pelos eventos de
// eurobank.antifraude.eventos, entregues às telas pelo circuito.
var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("eurobank-web");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddOptions<OpcoesFerramentas>().BindConfiguration(OpcoesFerramentas.Secao);
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

builder.Host.UseWolverine(opcoes => opcoes.ConfigurarMensageriaAntifraude(builder.Configuration));

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
