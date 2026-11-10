using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

public sealed class ExcecaoContaDestinoDesconhecida(string iban)
    : ExcecaoRegraNegocio(
        "conta_destino_desconhecida",
        $"Nenhuma conta EuroBank conhecida com o IBAN {iban}. Se acabou de ser aberta, tente de novo em instantes.");
