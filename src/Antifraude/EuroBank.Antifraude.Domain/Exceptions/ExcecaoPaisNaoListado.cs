using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoPaisNaoListado(string codigo)
    : ExcecaoNaoEncontrado("pais_nao_listado", $"O país {codigo} não está na lista de países de risco.");
