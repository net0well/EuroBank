using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace EuroBank.Antifraude.Painel.Services;

/// <summary>
/// Quem está usando o painel: o nome que o analista digitou, guardado no navegador (sem login). Um por circuito; vai em
/// cada ação (assumir, decidir, bloquear, confirmar fraude).
/// </summary>
public sealed class SessaoAnalista(ProtectedLocalStorage armazenamento)
{
    private const string Chave = "analista";

    public string? Nome { get; private set; }

    public bool Identificado => !string.IsNullOrWhiteSpace(Nome);

    public event Action? Mudou;

    /// <summary>Lê o nome guardado; só funciona depois da primeira renderização (precisa do navegador).</summary>
    public async Task CarregarAsync()
    {
        try
        {
            var guardado = await armazenamento.GetAsync<string>(Chave);
            Nome = guardado.Success ? guardado.Value : null;
        }
        catch (CryptographicException)
        {
            // Gravado com uma chave que o painel não tem mais (o container foi recriado e as chaves do Data Protection
            // ficavam nele): o analista digita o nome de novo.
            Nome = null;
            await armazenamento.DeleteAsync(Chave);
        }

        Mudou?.Invoke();
    }

    public async Task DefinirAsync(string? nome)
    {
        Nome = string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
        if (Nome is null)
        {
            await armazenamento.DeleteAsync(Chave);
        }
        else
        {
            await armazenamento.SetAsync(Chave, Nome);
        }

        Mudou?.Invoke();
    }
}
