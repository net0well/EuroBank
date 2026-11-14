using EuroBank.Antifraude.Api.Mappers;
using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Grpc;
using Grpc.Core;
using Wolverine;

namespace EuroBank.Antifraude.Api.Services;

/// <summary>
/// O serviço gRPC: traduz o contrato e delega à aplicação (Wolverine como mediator). Autenticação e tradução de erros
/// ficam nos interceptors; o mesmo código atende o JSON transcoding.
/// </summary>
public sealed class AntifraudeGrpcService(IMessageBus barramento, TimeProvider relogio) : AntifraudeService.AntifraudeServiceBase
{
    public override async Task<ResultadoAvaliacao> Avaliar(SolicitacaoAvaliacao request, ServerCallContext context) =>
        (await barramento.InvokeAsync<ResultadoAvaliacaoDto>(
            new AvaliarOperacao(request.ParaContexto(relogio.GetUtcNow())), context.CancellationToken)).ParaGrpc();

    public override async Task<ResultadoAvaliacao> ObterAnalise(ObterAnaliseRequest request, ServerCallContext context) =>
        (await barramento.InvokeAsync<ResultadoAvaliacaoDto>(
            new ObterAnalise(ConversaoGrpc.ParaAnaliseId(request.AnaliseId)), context.CancellationToken)).ParaGrpc();

    public override async Task<ResultadoAvaliacao> Simular(SolicitacaoAvaliacao request, ServerCallContext context) =>
        (await barramento.InvokeAsync<ResultadoAvaliacaoDto>(
            new SimularOperacao(request.ParaContexto(relogio.GetUtcNow())), context.CancellationToken)).ParaGrpc();
}
