using EuroBank.Antifraude.Grpc;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Infrastructure.Options;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace EuroBank.Transferencias.Infrastructure.Clients;

/// <summary>
/// Avaliar no antifraude, com o valor que vai sair da conta (na moeda dela, sem a tarifa). Qualquer falha da chamada
/// vira <see cref="ExcecaoAntifraudeIndisponivel"/>, e a saga decide entre tentar de novo e recusar.
/// </summary>
public sealed class AvaliacaoRiscoGrpc(AntifraudeService.AntifraudeServiceClient cliente, IOptions<OpcoesAntifraude> opcoes)
    : IAvaliacaoRisco
{
    public async Task<ResultadoRisco> AvaliarAsync(Transferencia transferencia, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var pedido = new SolicitacaoAvaliacao
        {
            OperacaoId = transferencia.Id.Valor.ToString(),
            TipoOperacao = transferencia.Tipo == TipoTransferencia.CambioInterno ? TipoOperacao.Cambio : TipoOperacao.Externa,
            ContaId = transferencia.ContaOrigemId.ToString(),
            Valor = transferencia.ValorDebitado!.Valor,
            Moeda = transferencia.MoedaOrigem.ToString(),
            IbanDestino = transferencia.IbanDestino.Valor,
            Momento = Timestamp.FromDateTimeOffset(agora),
        };

        ResultadoAvaliacao resposta;
        try
        {
            // O deadline é do relógio de parede, como o gRPC o envia ao servidor.
            resposta = await cliente.AvaliarAsync(
                pedido, deadline: DateTime.UtcNow + opcoes.Value.Deadline, cancellationToken: cancellationToken);
        }
        catch (RpcException erro) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExcecaoAntifraudeIndisponivel(erro.StatusCode.ToString(), erro);
        }

        return new ResultadoRisco(
            ParaDecisao(resposta.Decisao),
            Guid.Parse(resposta.AnaliseId),
            string.IsNullOrEmpty(resposta.CasoId) ? null : Guid.Parse(resposta.CasoId));
    }

    /// <summary>Uma decisão que a saga não conhece não aprova nada: conta como falha da chamada.</summary>
    private static DecisaoRisco ParaDecisao(Decisao decisao) => decisao switch
    {
        Decisao.Aprovada => DecisaoRisco.Aprovada,
        Decisao.EmRevisao => DecisaoRisco.EmRevisao,
        Decisao.Recusada => DecisaoRisco.Recusada,
        Decisao.Expirada => DecisaoRisco.Expirada,
        Decisao.Cancelada => DecisaoRisco.Cancelada,
        _ => throw new ExcecaoAntifraudeIndisponivel($"DecisaoDesconhecida({decisao})"),
    };
}
