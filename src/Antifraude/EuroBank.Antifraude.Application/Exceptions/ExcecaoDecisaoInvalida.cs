using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Application.Exceptions;

public sealed class ExcecaoDecisaoInvalida(string? decisao)
    : ExcecaoValidacao("decisao_invalida", $"Decisão '{decisao}' inválida: use Aprovar ou Recusar.");
