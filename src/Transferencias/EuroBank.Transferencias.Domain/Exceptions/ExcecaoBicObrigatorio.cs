using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoBicObrigatorio()
    : ExcecaoRegraNegocio("bic_obrigatorio", "Informe o BIC do banco de destino numa transferência externa.");
