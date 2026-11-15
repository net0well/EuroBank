using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using Wolverine;

namespace EuroBank.Antifraude.Api.Endpoints;

/// <summary>
/// O registro de modelos: versões com métricas e curva, promoção e sombra. As trocas valem em todas as instâncias da
/// API sem reiniciar. Uma versão só decide quando o analista a ativa; o challenger pontua, mas nunca decide.
/// </summary>
public static class ModelosEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsModelos(this IEndpointRouteBuilder app)
    {
        var modelos = app.MapGroup("/modelos").WithTags("Modelos");

        modelos.MapGet("", ListarAsync)
            .WithName("ListarModelos")
            .WithSummary("Versões do modelo, da mais nova para a mais antiga, com as métricas no teste temporal")
            .Produces<IReadOnlyList<VersaoModeloResumoDto>>();

        modelos.MapGet("/{versao:int}", ObterAsync)
            .WithName("ObterModelo")
            .WithSummary("Métricas, matriz de confusão, curva por limiar, dados do treino e distribuição de referência")
            .Produces<VersaoModeloDetalheDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        modelos.MapPost("/{versao:int}/ativacao", AtivarAsync)
            .WithName("AtivarModelo")
            .WithSummary("A versão passa a decidir; a ativa anterior é aposentada")
            .Produces<VersaoModeloResumoDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        modelos.MapPost("/{versao:int}/challenger", ColocarEmSombraAsync)
            .WithName("ColocarModeloEmSombra")
            .WithSummary("A versão pontua em sombra, sem decidir; o challenger anterior é aposentado")
            .Produces<VersaoModeloResumoDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        modelos.MapDelete("/challenger", RetirarChallengerAsync)
            .WithName("RetirarChallenger")
            .WithSummary("Aposenta o challenger, se houver")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListarAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<IReadOnlyList<VersaoModeloResumoDto>>(new ListarModelos(), cancellationToken));

    private static async Task<IResult> ObterAsync(int versao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<VersaoModeloDetalheDto>(new ObterModelo(versao), cancellationToken));

    private static async Task<IResult> AtivarAsync(int versao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<VersaoModeloResumoDto>(new AtivarModelo(versao), cancellationToken));

    private static async Task<IResult> ColocarEmSombraAsync(int versao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<VersaoModeloResumoDto>(new ColocarModeloEmSombra(versao), cancellationToken));

    private static async Task<IResult> RetirarChallengerAsync(IMessageBus barramento, CancellationToken cancellationToken)
    {
        await barramento.InvokeAsync(new RetirarChallenger(), cancellationToken);
        return TypedResults.NoContent();
    }
}
