using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Só decide quem assumiu o caso.</summary>
public sealed class ExcecaoCasoNaoAssumido()
    : ExcecaoRegraNegocio("caso_nao_assumido", "Assuma o caso antes de decidir.");
