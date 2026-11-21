using Bunit;
using EuroBank.Web.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;

namespace EuroBank.Web.Tests.Services;

/// <summary>O nome do analista guardado no navegador, protegido pelo Data Protection do servidor.</summary>
public sealed class SessaoAnalistaTestes : BunitContext
{
    private SessaoAnalista NovaSessao() =>
        new(new ProtectedLocalStorage(JSInterop.JSRuntime, new EphemeralDataProtectionProvider()));

    [Fact]
    public async Task NomeGravadoComOutraChave_SemNome_EApagaOValor()
    {
        // O que um container anterior gravou: as chaves dele não existem mais.
        JSInterop.Setup<string>("localStorage.getItem", _ => true).SetResult("CfDJ8-protegido-por-outra-chave");
        JSInterop.SetupVoid("localStorage.removeItem", _ => true).SetVoidResult();
        var sessao = NovaSessao();

        await sessao.CarregarAsync();

        sessao.Nome.ShouldBeNull();
        sessao.Identificado.ShouldBeFalse();
        JSInterop.VerifyInvoke("localStorage.removeItem");
    }

    [Fact]
    public async Task Definir_GuardaONomeSemEspacos()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var sessao = NovaSessao();

        await sessao.DefinirAsync("  Marta ");

        sessao.Nome.ShouldBe("Marta");
        JSInterop.VerifyInvoke("localStorage.setItem");
    }
}
