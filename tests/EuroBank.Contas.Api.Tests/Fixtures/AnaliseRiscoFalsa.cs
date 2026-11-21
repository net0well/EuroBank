using System.Collections.Concurrent;
using EuroBank.Contas.Application.Enums;
using EuroBank.Contas.Application.Exceptions;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.TransferObjects;

namespace EuroBank.Contas.Api.Tests.Fixtures;

/// <summary>
/// O antifraude de mentira para a API de Contas: responde a decisão escolhida por conta (aprovada, se nada foi
/// escolhido) ou falha como fora do ar, e guarda as operações que recebeu.
/// </summary>
public sealed class AnaliseRiscoFalsa : IAnaliseRisco
{
    private readonly ConcurrentDictionary<Guid, DecisaoRisco?> _decisoes = new();
    private readonly ConcurrentQueue<OperacaoEmAnalise> _recebidas = new();

    public IReadOnlyList<OperacaoEmAnalise> Recebidas => [.. _recebidas];

    public void Responder(Guid contaId, DecisaoRisco decisao) => _decisoes[contaId] = decisao;

    /// <summary>Nulo: a chamada falha como se o antifraude estivesse fora do ar.</summary>
    public void ForaDoAr(Guid contaId) => _decisoes[contaId] = null;

    public Task<ResultadoRisco> AvaliarAsync(OperacaoEmAnalise operacao, CancellationToken cancellationToken)
    {
        _recebidas.Enqueue(operacao);
        if (!_decisoes.TryGetValue(operacao.ContaId, out var decisao))
        {
            return Task.FromResult(new ResultadoRisco(DecisaoRisco.Aprovada, Guid.CreateVersion7(), null));
        }

        return decisao is { } escolhida
            ? Task.FromResult(new ResultadoRisco(escolhida, Guid.CreateVersion7(), escolhida == DecisaoRisco.EmRevisao ? Guid.CreateVersion7() : null))
            : throw new ExcecaoAntifraudeIndisponivel("Unavailable");
    }
}
