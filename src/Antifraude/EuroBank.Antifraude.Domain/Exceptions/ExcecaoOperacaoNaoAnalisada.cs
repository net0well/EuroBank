using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoOperacaoNaoAnalisada(Guid operacaoId)
    : ExcecaoNaoEncontrado("operacao_nao_analisada", $"A operação {operacaoId} não tem análise do antifraude.");
