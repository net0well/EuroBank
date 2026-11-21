using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Uma operação que o status atual da execução não permite (iniciar uma que já começou, por exemplo).</summary>
public sealed class ExcecaoTransicaoTreinoInvalida(StatusTreino status, string operacao)
    : ExcecaoConflito("transicao_treino_invalida", $"Um treino {status} não pode {operacao}.");
