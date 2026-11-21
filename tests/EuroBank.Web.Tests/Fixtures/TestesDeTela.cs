using Bunit;
using EuroBank.Web.Antifraude.Clients;
using EuroBank.Web.Antifraude.Services;
using EuroBank.Web.Options;
using EuroBank.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace EuroBank.Web.Tests.Fixtures;

/// <summary>
/// Base dos testes de tela com bUnit: MudBlazor, a API falsa no lugar da REST, os avisos da fila e a sessão do
/// analista (o armazenamento do navegador fica de mentira, em modo solto). As telas são renderizadas com o
/// MudPopoverProvider ao lado, como no layout.
/// </summary>
public abstract class TestesDeTela : BunitContext
{
    protected TestesDeTela()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(new ClienteAntifraude(Api.CriarCliente()));
        Services.AddSingleton(Avisos);
        Services.AddSingleton(new ProtectedLocalStorage(JSInterop.JSRuntime, new EphemeralDataProtectionProvider()));
        Services.AddScoped<SessaoAnalista>();
        Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(Ferramentas));
    }

    /// <summary>Os endereços das ferramentas que o início lista (os padrões, com o Mailpit numa porta trocada).</summary>
    protected OpcoesFerramentas Ferramentas { get; } = new() { Mailpit = new Uri("http://localhost:8026") };

    protected ApiFalsa Api { get; } = new();

    protected AvisosFila Avisos { get; } = new(NullLogger<AvisosFila>.Instance);

    protected static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    /// <summary>Renderiza a tela com o provider de popovers que o MudSelect e os menus exigem.</summary>
    protected IRenderedComponent<TTela> RenderizarTela<TTela>(IReadOnlyDictionary<string, object?>? parametros = null)
        where TTela : IComponent
    {
        var raiz = Render(construtor =>
        {
            construtor.OpenComponent<MudPopoverProvider>(0);
            construtor.CloseComponent();
            construtor.OpenComponent<TTela>(1);
            foreach (var (nome, valor) in parametros ?? new Dictionary<string, object?>())
            {
                construtor.AddAttribute(2, nome, valor);
            }

            construtor.CloseComponent();
        });
        return raiz.FindComponent<TTela>();
    }

    protected async Task IdentificarAsync(string nome) => await Services.GetRequiredService<SessaoAnalista>().DefinirAsync(nome);
}
