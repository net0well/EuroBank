using EuroBank.Antifraude.Grpc;
using EuroBank.Contas.Application.Enums;
using EuroBank.Contas.Application.Exceptions;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Infrastructure.Options;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using DecisaoRiscoContas = EuroBank.Contas.Application.Enums.DecisaoRisco;
using LocalizacaoGrpc = EuroBank.Antifraude.Grpc.Localizacao;

namespace EuroBank.Contas.Infrastructure.Clients;

/// <summary>
/// Avaliar no antifraude pelo gRPC, com o prazo das opções. Qualquer falha da chamada vira
/// <see cref="ExcecaoAntifraudeIndisponivel"/>: sem resposta, a operação não executa.
/// </summary>
public sealed class AnaliseRiscoGrpc(AntifraudeService.AntifraudeServiceClient cliente, IOptions<OpcoesAntifraude> opcoes) : IAnaliseRisco
{
    public async Task<ResultadoRisco> AvaliarAsync(OperacaoEmAnalise operacao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        var pedido = new SolicitacaoAvaliacao
        {
            OperacaoId = operacao.OperacaoId.ToString(),
            TipoOperacao = operacao.Tipo == TipoOperacaoRisco.Saque ? TipoOperacao.Saque : TipoOperacao.TransferenciaInterna,
            ContaId = operacao.ContaId.ToString(),
            Valor = operacao.Valor.Valor,
            Moeda = operacao.Valor.Moeda.ToString(),
            IbanDestino = operacao.IbanDestino ?? string.Empty,
            Momento = Timestamp.FromDateTimeOffset(operacao.Momento),
            DispositivoId = operacao.DispositivoId ?? string.Empty,
            Localizacao = operacao.Localizacao is { } localizacao
                ? new LocalizacaoGrpc { Latitude = localizacao.Latitude, Longitude = localizacao.Longitude, Pais = localizacao.Pais }
                : null,
        };

        ResultadoAvaliacao resposta;
        try
        {
            resposta = await cliente.AvaliarAsync(pedido, deadline: DateTime.UtcNow + opcoes.Value.Deadline, cancellationToken: cancellationToken);
        }
        catch (RpcException erro) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExcecaoAntifraudeIndisponivel(erro.StatusCode.ToString());
        }

        var decisao = resposta.Decisao switch
        {
            Decisao.Aprovada => DecisaoRiscoContas.Aprovada,
            Decisao.EmRevisao => DecisaoRiscoContas.EmRevisao,
            _ => DecisaoRiscoContas.Recusada,
        };
        return new ResultadoRisco(
            decisao,
            Guid.TryParse(resposta.AnaliseId, out var analise) ? analise : null,
            Guid.TryParse(resposta.CasoId, out var caso) ? caso : null);
    }
}
