using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoBicInvalido(string bic)
    : ExcecaoRegraNegocio("bic_invalido", $"BIC '{bic}' inválido: use 8 ou 11 caracteres (ex.: DEUTDEFF ou DEUTDEFF500).");
