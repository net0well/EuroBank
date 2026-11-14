using EuroBank.Antifraude.Api.TransferObjects;
using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Exceptions;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.TransferObjects;
using Wolverine;

namespace EuroBank.Antifraude.Api.Endpoints;

/// <summary>A fila de revisão do analista: listar, detalhar, assumir e decidir casos.</summary>
public static class CasosEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsCasos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/casos").WithTags("Casos de revisão");

        grupo.MapGet("", ListarAsync)
            .WithName("ListarCasos")
            .WithSummary("Sem status: os pendentes (abertos e em análise), os que expiram primeiro no topo")
            .Produces<ResultadoPaginado<CasoResumoDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapGet("/{id:guid}", ObterAsync)
            .WithName("ObterCaso")
            .WithSummary("O caso com a análise (operação, sinais de cada regra, score) e a auditoria")
            .Produces<CasoDetalheDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/{id:guid}/atribuicao", AssumirAsync)
            .WithName("AssumirCaso")
            .WithSummary("O analista assume o caso (identificado pelo nome)")
            .Produces<CasoDetalheDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/{id:guid}/decisao", DecidirAsync)
            .WithName("DecidirCaso")
            .WithSummary("Aprovar ou Recusar com justificativa (ao recusar, pode bloquear o IBAN); a saga reage")
            .Produces<CasoDetalheDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> ListarAsync(
        string? status, int? pagina, int? tamanho, IMessageBus barramento, CancellationToken cancellationToken)
    {
        EstadoCaso? estado = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            estado = Enum.TryParse<EstadoCaso>(status, ignoreCase: true, out var valor) && Enum.IsDefined(valor)
                ? valor
                : throw new ExcecaoFiltroInvalido("status", status);
        }

        return TypedResults.Ok(await barramento.InvokeAsync<ResultadoPaginado<CasoResumoDto>>(
            new ListarCasos(estado, pagina, tamanho), cancellationToken));
    }

    private static async Task<IResult> ObterAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<CasoDetalheDto>(new ObterCaso(id), cancellationToken));

    private static async Task<IResult> AssumirAsync(
        Guid id, AtribuicaoRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<CasoDetalheDto>(
            new AssumirCaso(id, requisicao.Analista ?? string.Empty), cancellationToken));

    private static async Task<IResult> DecidirAsync(
        Guid id, DecisaoRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken)
    {
        var aprovar = requisicao.Decisao?.Trim().ToUpperInvariant() switch
        {
            "APROVAR" => true,
            "RECUSAR" => false,
            _ => throw new ExcecaoDecisaoInvalida(requisicao.Decisao),
        };

        return TypedResults.Ok(await barramento.InvokeAsync<CasoDetalheDto>(
            new DecidirCaso(id, requisicao.Analista ?? string.Empty, aprovar, requisicao.Justificativa ?? string.Empty, requisicao.BloquearIban),
            cancellationToken));
    }
}
