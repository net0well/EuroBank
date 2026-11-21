using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoParametrosTreinoInvalidos(string parametro, string faixa)
    : ExcecaoValidacao("parametros_treino_invalidos", $"O parâmetro {parametro} do treino deve ficar {faixa}.");
