using System.Reflection;
using EuroBank.Web.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace EuroBank.Web.Tests.Components;

/// <summary>Cada área tem as rotas sob o próprio prefixo, o início fica na raiz e nenhuma rota se repete.</summary>
public sealed class RotasTestes
{
    private static readonly Assembly Web = typeof(PaginaInicio).Assembly;

    private static IEnumerable<(Type Tela, string Rota)> Rotas() =>
        Web.GetTypes()
            .Where(tipo => typeof(IComponent).IsAssignableFrom(tipo))
            .SelectMany(tipo => tipo.GetCustomAttributes<RouteAttribute>().Select(rota => (tipo, rota.Template)));

    [Fact]
    public void TelasDoAntifraude_FicamSobAntifraude()
    {
        var antifraude = Rotas().Where(rota => rota.Tela.Namespace == "EuroBank.Web.Components.Pages.Antifraude").ToList();

        antifraude.Count.ShouldBe(8);
        antifraude.ShouldAllBe(rota => rota.Rota == "/antifraude" || rota.Rota.StartsWith("/antifraude/", StringComparison.Ordinal));
    }

    [Fact]
    public void Inicio_NaRaiz_ESemRotaRepetida()
    {
        var rotas = Rotas().ToList();

        rotas.Single(rota => rota.Rota == "/").Tela.ShouldBe(typeof(PaginaInicio));
        rotas.Select(rota => rota.Rota).ShouldBeUnique();
    }
}
