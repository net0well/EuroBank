using EuroBank.Shared.Exceptions;
using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Respostas do banco externo: confirmação, recusa e status da consulta.</summary>
public sealed class RespostasBancoExternoManipulador(AplicadorRespostas aplicador, PoliticaSaga politica)
{
    public Task Handle(LiquidacaoConfirmada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(LiquidacaoConfirmada),
            (transferencia, agora) => transferencia.RegistrarLiquidacaoConfirmada(agora),
            cancellationToken);

    public Task Handle(LiquidacaoRecusada resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(LiquidacaoRecusada),
            (transferencia, agora) => transferencia.RegistrarLiquidacaoRecusada(politica, agora),
            cancellationToken);

    public Task Handle(StatusLiquidacao resposta, CancellationToken cancellationToken)
    {
        // Validado antes de carregar a saga: um status desconhecido vai para o dead letter, não vira estorno.
        var status = StatusDe(resposta.Status);
        return aplicador.AplicarAsync(
            resposta.TransferenciaId,
            $"{nameof(StatusLiquidacao)} ({status})",
            (transferencia, agora) => transferencia.RegistrarStatusLiquidacao(status, politica, agora),
            cancellationToken);
    }

    private static StatusLiquidacaoExterna StatusDe(string status) =>
        Enum.TryParse<StatusLiquidacaoExterna>(status, ignoreCase: false, out var lido)
        && Enum.IsDefined(lido)
        && !int.TryParse(status, out _)
            ? lido
            : throw new ExcecaoMensagemInvalida($"status de liquidação desconhecido '{status}'");
}
