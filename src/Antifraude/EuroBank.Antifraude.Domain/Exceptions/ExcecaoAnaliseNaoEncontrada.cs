using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoAnaliseNaoEncontrada(Guid analiseId)
    : ExcecaoNaoEncontrado("analise_nao_encontrada", $"Análise {analiseId} não encontrada.");
