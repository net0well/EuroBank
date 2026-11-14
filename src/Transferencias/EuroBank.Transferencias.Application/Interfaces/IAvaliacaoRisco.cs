using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Interfaces;

/// <summary>
/// O antifraude, pelo gRPC. A chamada é idempotente pelo id da transferência: repetida (na revisão, por exemplo),
/// devolve a mesma análise com a decisão atual.
/// </summary>
public interface IAvaliacaoRisco
{
    /// <exception cref="Exceptions.ExcecaoAntifraudeIndisponivel">O antifraude não respondeu ou recusou a chamada.</exception>
    Task<ResultadoRisco> AvaliarAsync(Transferencia transferencia, DateTimeOffset agora, CancellationToken cancellationToken);
}
