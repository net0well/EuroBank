using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Um treino por vez: há outro solicitado ou em andamento.</summary>
public sealed class ExcecaoTreinoEmAndamento()
    : ExcecaoConflito("treino_em_andamento", "Já há um treino solicitado ou em andamento: espere ele terminar ou cancele-o.");
