using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoIbanNaoBloqueado(string iban)
    : ExcecaoNaoEncontrado("iban_nao_bloqueado", $"O IBAN {iban} não está na lista de bloqueio.");
