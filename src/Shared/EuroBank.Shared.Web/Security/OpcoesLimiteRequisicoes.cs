using System.ComponentModel.DataAnnotations;

namespace EuroBank.Shared.Web.Security;

/// <summary>Seção "LimiteRequisicoes". No compose, PorSegundo vem de RATE_LIMIT_POR_SEGUNDO (o mesmo nas quatro APIs).</summary>
public sealed class OpcoesLimiteRequisicoes
{
    public const string Secao = "LimiteRequisicoes";

    public bool Ativo { get; set; } = true;

    /// <summary>Requisições por API key num segundo, em janela deslizante.</summary>
    [Range(1, int.MaxValue)]
    public int PorSegundo { get; set; } = 100;
}
