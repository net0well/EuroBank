using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoTreinoTerminado(Guid id, StatusTreino status)
    : ExcecaoConflito("treino_terminado", $"O treino {id} já terminou ({status}).");
