using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Bloquear o IBAN só faz sentido ao recusar.</summary>
public sealed class ExcecaoBloqueioSemRecusa()
    : ExcecaoValidacao("bloqueio_sem_recusa", "O IBAN só pode ser bloqueado junto com a recusa do caso.");
