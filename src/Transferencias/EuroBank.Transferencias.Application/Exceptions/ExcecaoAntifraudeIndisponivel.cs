namespace EuroBank.Transferencias.Application.Exceptions;

/// <summary>
/// A chamada ao antifraude falhou: fora do ar, prazo estourado ou chamada recusada. A saga tenta de novo até o prazo
/// da análise e então recusa a transferência (fail-closed). <see cref="Exception.Message"/> é o status da chamada.
/// </summary>
public sealed class ExcecaoAntifraudeIndisponivel(string status, Exception? causa = null) : Exception(status, causa);
