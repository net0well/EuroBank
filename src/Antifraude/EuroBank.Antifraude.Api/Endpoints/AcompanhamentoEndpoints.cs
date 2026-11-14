using EuroBank.Antifraude.Api.TransferObjects;
using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using Wolverine;

namespace EuroBank.Antifraude.Api.Endpoints;

/// <summary>Fraudes confirmadas depois do fato (rótulos para o retreino) e a visão geral das últimas 24 horas.</summary>
public static class AcompanhamentoEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsAcompanhamento(this IEndpointRouteBuilder app)
    {
        app.MapPost("/fraudes-confirmadas", ConfirmarFraudeAsync)
            .WithTags("Acompanhamento")
            .WithName("ConfirmarFraude")
            .WithSummary("Marca uma operação analisada como fraude (marcar de novo devolve a marcação original)")
            .Produces<FraudeConfirmadaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/visao-geral", VisaoGeralAsync)
            .WithTags("Acompanhamento")
            .WithName("ObterVisaoGeral")
            .WithSummary("Análises por decisão, taxas de revisão e recusa, casos e regras que mais dispararam (24 h)")
            .Produces<VisaoGeralDto>();

        return app;
    }

    private static async Task<IResult> ConfirmarFraudeAsync(
        FraudeConfirmadaRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<FraudeConfirmadaDto>(
            new ConfirmarFraude(requisicao.OperacaoId, requisicao.Analista ?? string.Empty, requisicao.Observacao), cancellationToken));

    private static async Task<IResult> VisaoGeralAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<VisaoGeralDto>(new ObterVisaoGeral(), cancellationToken));
}
