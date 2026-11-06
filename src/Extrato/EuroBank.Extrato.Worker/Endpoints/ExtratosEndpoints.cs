using EuroBank.Extrato.Application.Queries;
using EuroBank.Extrato.Application.TransferObjects;
using Wolverine;

namespace EuroBank.Extrato.Worker.Endpoints;

public static class ExtratosEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsExtratos(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/extratos")
            .WithTags("Extratos")
            .MapGet("/{contaId:guid}", ObterAsync)
            .WithName("ObterExtrato")
            .WithSummary("Extrato da conta, com os lançamentos do mais recente para o mais antigo")
            .Produces<ExtratoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ObterAsync(
        Guid contaId, int? pagina, int? tamanho, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ExtratoDto>(
            new ObterExtrato(contaId, pagina, tamanho), cancellationToken));
}
