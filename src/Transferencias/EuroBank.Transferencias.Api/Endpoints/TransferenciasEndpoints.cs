using EuroBank.Shared.TransferObjects;
using EuroBank.Transferencias.Api.TransferObjects;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Queries;
using EuroBank.Transferencias.Application.TransferObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace EuroBank.Transferencias.Api.Endpoints;

/// <summary>Endpoints finos: traduzem HTTP em comandos/consultas e delegam ao Wolverine (mediator).</summary>
public static class TransferenciasEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsTransferencias(this IEndpointRouteBuilder app)
    {
        app.MapPost("/transferencias-externas", SolicitarExternaAsync)
            .WithTags("Transferências")
            .WithName("SolicitarTransferenciaExterna")
            .WithSummary("Pede uma transferência para um IBAN de outro banco; a saga roda em segundo plano")
            .Produces<TransferenciaDto>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPost("/transferencias-cambio", SolicitarCambioAsync)
            .WithTags("Transferências")
            .WithName("SolicitarTransferenciaCambio")
            .WithSummary("Pede um câmbio para outra conta EuroBank em outra moeda; a saga cota, debita e credita")
            .Produces<TransferenciaDto>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPost("/transferencias-externas/{id:guid}/reconciliacao", ReconciliarAsync)
            .WithTags("Transferências")
            .WithName("ReconciliarTransferencia")
            .WithSummary("Reconcilia uma Divergente (banco confirmou depois do estorno): debita de novo valor e tarifa")
            .Produces<TransferenciaDto>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        var grupo = app.MapGroup("/transferencias").WithTags("Transferências");

        grupo.MapGet("/{id:guid}", ObterAsync)
            .WithName("ObterTransferencia")
            .WithSummary("Estado, motivo, prazo do passo atual e histórico de passos da saga")
            .Produces<TransferenciaDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapGet("", ListarAsync)
            .WithName("ListarTransferencias")
            .WithSummary("Lista as transferências, das mais recentes para as mais antigas")
            .Produces<ResultadoPaginado<TransferenciaResumoDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapPost("/{id:guid}/cancelamento", CancelarAsync)
            .WithName("CancelarTransferencia")
            .WithSummary("Cancela até a liquidação ser pedida; com o débito em andamento, a saga estorna")
            .Produces<TransferenciaDto>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> SolicitarExternaAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        TransferenciaExternaRequisicao requisicao,
        IMessageBus barramento,
        CancellationToken cancellationToken)
    {
        var transferencia = await barramento.InvokeAsync<TransferenciaDto>(
            new SolicitarTransferenciaExterna(
                idempotencyKey,
                requisicao.ContaOrigemId,
                requisicao.IbanDestino,
                requisicao.Bic,
                requisicao.NomeBeneficiario,
                requisicao.Valor,
                requisicao.ExecutarEm,
                requisicao.Descricao,
                requisicao.EmailNotificacao,
                requisicao.MoedaDoValor,
                requisicao.MoedaDestino),
            cancellationToken);

        return TypedResults.Accepted($"/transferencias/{transferencia.Id}", transferencia);
    }

    private static async Task<IResult> SolicitarCambioAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        TransferenciaCambioRequisicao requisicao,
        IMessageBus barramento,
        CancellationToken cancellationToken)
    {
        var transferencia = await barramento.InvokeAsync<TransferenciaDto>(
            new SolicitarTransferenciaCambio(
                idempotencyKey,
                requisicao.ContaOrigemId,
                requisicao.IbanDestino,
                requisicao.Valor,
                requisicao.MoedaDoValor,
                requisicao.ExecutarEm,
                requisicao.Descricao,
                requisicao.EmailNotificacao),
            cancellationToken);

        return TypedResults.Accepted($"/transferencias/{transferencia.Id}", transferencia);
    }

    private static async Task<IResult> ObterAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<TransferenciaDto>(new ObterTransferencia(id), cancellationToken));

    private static async Task<IResult> ListarAsync(
        Guid? contaOrigemId,
        string? tipo,
        string? status,
        int? pagina,
        int? tamanho,
        IMessageBus barramento,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ResultadoPaginado<TransferenciaResumoDto>>(
            new ListarTransferencias(contaOrigemId, tipo, status, pagina, tamanho), cancellationToken));

    private static async Task<IResult> CancelarAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken)
    {
        var transferencia = await barramento.InvokeAsync<TransferenciaDto>(new CancelarTransferencia(id), cancellationToken);
        return TypedResults.Accepted($"/transferencias/{transferencia.Id}", transferencia);
    }

    private static async Task<IResult> ReconciliarAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken)
    {
        var transferencia = await barramento.InvokeAsync<TransferenciaDto>(new ReconciliarTransferencia(id), cancellationToken);
        return TypedResults.Accepted($"/transferencias/{transferencia.Id}", transferencia);
    }
}
