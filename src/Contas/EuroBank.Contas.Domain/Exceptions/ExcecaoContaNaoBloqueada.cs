using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoContaNaoBloqueada()
    : ExcecaoRegraNegocio("conta_nao_bloqueada", "Só uma conta bloqueada pode ser desbloqueada.");
