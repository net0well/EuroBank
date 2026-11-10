using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

/// <summary>A conta ainda não chegou à projeção local (consistência eventual) ou não existe.</summary>
public sealed class ExcecaoContaOrigemDesconhecida(Guid contaId)
    : ExcecaoRegraNegocio(
        "conta_origem_desconhecida",
        $"A conta {contaId} ainda não é conhecida por Transferências. Se acabou de ser aberta, tente de novo em instantes.");
