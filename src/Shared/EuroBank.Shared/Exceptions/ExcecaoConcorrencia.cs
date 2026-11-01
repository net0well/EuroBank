namespace EuroBank.Shared.Exceptions;

/// <summary>Outra operação alterou o agregado entre a leitura e a gravação (concorrência otimista).</summary>
public sealed class ExcecaoConcorrencia()
    : ExcecaoDominio("conflito_de_concorrencia", "O registro foi alterado por outra operação. Tente novamente.");
