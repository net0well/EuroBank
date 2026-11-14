using EuroBank.Antifraude.Api.TransferObjects;
using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using Wolverine;

namespace EuroBank.Antifraude.Api.Endpoints;

/// <summary>IBANs bloqueados (regra dura: recusa) e países de risco (regra com peso).</summary>
public static class ListasEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsListas(this IEndpointRouteBuilder app)
    {
        var bloqueio = app.MapGroup("/listas/bloqueio").WithTags("Listas");

        bloqueio.MapGet("", ListarIbansAsync).WithName("ListarIbansBloqueados").Produces<IReadOnlyList<IbanBloqueadoDto>>();

        bloqueio.MapPost("", BloquearAsync)
            .WithName("BloquearIban")
            .WithSummary("Bloqueia o IBAN (já bloqueado: devolve o bloqueio existente)")
            .Produces<IbanBloqueadoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        bloqueio.MapDelete("/{iban}", DesbloquearAsync)
            .WithName("DesbloquearIban")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var paises = app.MapGroup("/listas/paises-risco").WithTags("Listas");

        paises.MapGet("", ListarPaisesAsync).WithName("ListarPaisesRisco").Produces<IReadOnlyList<PaisRiscoDto>>();

        paises.MapPost("", IncluirPaisAsync)
            .WithName("IncluirPaisRisco")
            .WithSummary("Inclui o país (já incluído: devolve o existente)")
            .Produces<PaisRiscoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        paises.MapDelete("/{codigo}", RemoverPaisAsync)
            .WithName("RemoverPaisRisco")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListarIbansAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<IReadOnlyList<IbanBloqueadoDto>>(new ListarIbansBloqueados(), cancellationToken));

    private static async Task<IResult> BloquearAsync(BloqueioIbanRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<IbanBloqueadoDto>(
            new BloquearIban(requisicao.Iban ?? string.Empty, requisicao.Motivo, requisicao.Analista ?? string.Empty), cancellationToken));

    private static async Task<IResult> DesbloquearAsync(string iban, IMessageBus barramento, CancellationToken cancellationToken)
    {
        await barramento.InvokeAsync(new DesbloquearIban(iban), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListarPaisesAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<IReadOnlyList<PaisRiscoDto>>(new ListarPaisesRisco(), cancellationToken));

    private static async Task<IResult> IncluirPaisAsync(PaisRiscoRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<PaisRiscoDto>(new IncluirPaisRisco(requisicao.Codigo ?? string.Empty), cancellationToken));

    private static async Task<IResult> RemoverPaisAsync(string codigo, IMessageBus barramento, CancellationToken cancellationToken)
    {
        await barramento.InvokeAsync(new RemoverPaisRisco(codigo), cancellationToken);
        return TypedResults.NoContent();
    }
}
