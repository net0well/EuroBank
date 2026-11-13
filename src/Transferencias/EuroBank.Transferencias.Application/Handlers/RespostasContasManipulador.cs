using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Respostas de Contas aos comandos da saga (débito, crédito, estorno, reconciliação e devolução).</summary>
public sealed class RespostasContasManipulador(AplicadorRespostas aplicador, PoliticaSaga politica)
{
    public Task Handle(DebitoRealizado resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(DebitoRealizado),
            (transferencia, agora) => transferencia.RegistrarDebitoRealizado(politica, agora),
            cancellationToken);

    public Task Handle(DebitoRecusado resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(DebitoRecusado),
            (transferencia, agora) => transferencia.RegistrarDebitoRecusado(MotivoDe(resposta.Motivo), agora),
            cancellationToken);

    public Task Handle(EstornoRealizado resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(EstornoRealizado),
            (transferencia, agora) => transferencia.RegistrarEstornoRealizado(agora),
            cancellationToken);

    public Task Handle(CreditoRealizado resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(CreditoRealizado),
            (transferencia, agora) => transferencia.RegistrarCreditoRealizado(agora),
            cancellationToken);

    public Task Handle(CreditoRecusado resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(CreditoRecusado),
            (transferencia, agora) => transferencia.RegistrarCreditoRecusado(MotivoDe(resposta.Motivo), politica, agora),
            cancellationToken);

    public Task Handle(ReconciliacaoRealizada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(ReconciliacaoRealizada),
            (transferencia, agora) => transferencia.RegistrarReconciliacaoRealizada(agora),
            cancellationToken);

    public Task Handle(ReconciliacaoRecusada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(ReconciliacaoRecusada),
            (transferencia, agora) => transferencia.RegistrarReconciliacaoRecusada(MotivoDe(resposta.Motivo), agora),
            cancellationToken);

    public Task Handle(DevolucaoCreditada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(DevolucaoCreditada),
            (transferencia, agora) => transferencia.RegistrarDevolucaoCreditada(agora),
            cancellationToken);

    public Task Handle(DevolucaoRecusada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(DevolucaoRecusada),
            (transferencia, agora) => transferencia.RegistrarDevolucaoRecusada(MotivoDe(resposta.Motivo), agora),
            cancellationToken);

    /// <summary>Os motivos de Contas têm o mesmo nome dos da saga; um motivo novo não trava a saga.</summary>
    public static MotivoTransferencia MotivoDe(string motivo) =>
        Enum.TryParse<MotivoTransferencia>(motivo, ignoreCase: false, out var lido)
        && Enum.IsDefined(lido)
        && !int.TryParse(motivo, out _)
            ? lido
            : MotivoTransferencia.RecusadaPorContas;
}
