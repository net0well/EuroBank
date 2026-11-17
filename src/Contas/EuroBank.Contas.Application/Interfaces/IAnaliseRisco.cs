using EuroBank.Contas.Application.TransferObjects;

namespace EuroBank.Contas.Application.Interfaces;

/// <summary>O antifraude, perguntado antes do saque e da transferência interna.</summary>
public interface IAnaliseRisco
{
    /// <exception cref="Exceptions.ExcecaoAntifraudeIndisponivel">Sem resposta (fora do ar, prazo vencido).</exception>
    Task<ResultadoRisco> AvaliarAsync(OperacaoEmAnalise operacao, CancellationToken cancellationToken);
}
