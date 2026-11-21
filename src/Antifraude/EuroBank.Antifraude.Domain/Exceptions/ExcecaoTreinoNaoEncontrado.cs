using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoTreinoNaoEncontrado(Guid id)
    : ExcecaoNaoEncontrado("treino_nao_encontrado", $"O treino {id} não existe.");
