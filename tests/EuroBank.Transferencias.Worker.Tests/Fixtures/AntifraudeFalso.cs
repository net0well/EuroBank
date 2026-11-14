using System.Collections.Concurrent;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Worker.Tests.Fixtures;

/// <summary>
/// O antifraude no lugar do cliente gRPC. Cada teste diz o que responder para a sua transferência; sem resposta
/// combinada, a chamada falha como o antifraude fora do ar. A análise tem um id fixo por transferência, como o
/// Avaliar idempotente faria.
/// </summary>
public sealed class AntifraudeFalso : IAvaliacaoRisco
{
    private readonly ConcurrentDictionary<TransferenciaId, (DecisaoRisco Decisao, Guid? CasoId)> _respostas = new();

    private readonly ConcurrentDictionary<TransferenciaId, Guid> _analises = new();

    private readonly ConcurrentDictionary<TransferenciaId, List<decimal>> _valores = new();

    public Guid AnaliseDe(TransferenciaId id) => _analises.GetOrAdd(id, _ => Guid.CreateVersion7());

    /// <summary>Valores com que a transferência foi enviada ao antifraude, um por chamada.</summary>
    public IReadOnlyList<decimal> ValoresEnviados(TransferenciaId id) => _valores.TryGetValue(id, out var valores) ? valores : [];

    public void Responder(TransferenciaId id, DecisaoRisco decisao, Guid? casoId = null) => _respostas[id] = (decisao, casoId);

    /// <summary>Daqui em diante, as chamadas desta transferência falham como o antifraude fora do ar.</summary>
    public void ForaDoAr(TransferenciaId id) => _respostas.TryRemove(id, out _);

    public Task<ResultadoRisco> AvaliarAsync(Transferencia transferencia, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        _valores.GetOrAdd(transferencia.Id, _ => []).Add(transferencia.ValorDebitado!.Valor);
        if (!_respostas.TryGetValue(transferencia.Id, out var resposta))
        {
            throw new ExcecaoAntifraudeIndisponivel("Unavailable");
        }

        return Task.FromResult(new ResultadoRisco(resposta.Decisao, AnaliseDe(transferencia.Id), resposta.CasoId));
    }
}
