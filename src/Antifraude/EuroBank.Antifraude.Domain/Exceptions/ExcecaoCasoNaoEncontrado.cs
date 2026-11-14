using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoCasoNaoEncontrado(Guid casoId)
    : ExcecaoNaoEncontrado("caso_nao_encontrado", $"Caso {casoId} não encontrado.");
