using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>O IBAN informado para a lista de bloqueio não tem o formato de um IBAN.</summary>
public sealed class ExcecaoIbanInvalido(string iban)
    : ExcecaoValidacao("iban_invalido", $"IBAN inválido: '{iban}'.");
