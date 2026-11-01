using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoIbanNaoEncontrado(Iban iban)
    : ExcecaoNaoEncontrado("iban_nao_encontrado", $"Não existe conta com o IBAN {iban}.");
