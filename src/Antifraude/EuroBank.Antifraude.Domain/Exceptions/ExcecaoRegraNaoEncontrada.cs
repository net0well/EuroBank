using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoRegraNaoEncontrada(string codigo)
    : ExcecaoNaoEncontrado("regra_nao_encontrada", $"Regra {codigo} não encontrada.");
