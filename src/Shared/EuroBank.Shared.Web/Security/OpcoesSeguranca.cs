using System.ComponentModel.DataAnnotations;

namespace EuroBank.Shared.Web.Security;

/// <summary>Seção "Seguranca" do appsettings. No compose, vem de CONTAS_API_KEY, EXTRATO_API_KEY etc.</summary>
public sealed class OpcoesSeguranca
{
    public const string Secao = "Seguranca";

    public const string HeaderApiKey = "X-Api-Key";

    /// <summary>Chave que os clientes devem mandar no header X-Api-Key.</summary>
    [Required]
    [MinLength(8)]
    public string ApiKey { get; set; } = string.Empty;
}
