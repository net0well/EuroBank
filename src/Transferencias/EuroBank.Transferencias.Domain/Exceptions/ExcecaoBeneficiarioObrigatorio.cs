using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoBeneficiarioObrigatorio()
    : ExcecaoValidacao("beneficiario_obrigatorio", "Informe o nome do beneficiário.");
