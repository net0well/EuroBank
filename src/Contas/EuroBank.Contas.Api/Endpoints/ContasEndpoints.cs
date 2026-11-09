using EuroBank.Contas.Api.TransferObjects;
using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Queries;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Shared.TransferObjects;
using Wolverine;

namespace EuroBank.Contas.Api.Endpoints;

/// <summary>Endpoints finos: traduzem HTTP em comandos/consultas e delegam ao Wolverine (mediator).</summary>
public static class ContasEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsContas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/contas").WithTags("Contas");

        grupo.MapPost("", AbrirAsync)
            .WithName("AbrirConta")
            .WithSummary("Abre uma conta ativa com saldo zero, na moeda escolhida (padrão EUR)")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapGet("", ListarAsync)
            .WithName("ListarContas")
            .WithSummary("Lista as contas, das mais recentes para as mais antigas")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapGet("/{id:guid}", ObterAsync)
            .WithName("ObterConta")
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/{id:guid}/depositos", DepositarAsync)
            .WithName("Depositar")
            .WithSummary("Credita um valor (aceito também em conta bloqueada)")
            .ProducesOperacao();

        grupo.MapPost("/{id:guid}/saques", SacarAsync)
            .WithName("Sacar")
            .ProducesOperacao();

        grupo.MapPost("/{id:guid}/transferencias", TransferirAsync)
            .WithName("Transferir")
            .WithSummary("Transfere para o IBAN de destino; devolve a conta de origem")
            .ProducesOperacao();

        grupo.MapPost("/{id:guid}/bloqueio", BloquearAsync)
            .WithName("BloquearConta")
            .ProducesOperacao();

        grupo.MapPost("/{id:guid}/desbloqueio", DesbloquearAsync)
            .WithName("DesbloquearConta")
            .ProducesOperacao();

        grupo.MapPost("/{id:guid}/encerramento", EncerrarAsync)
            .WithName("EncerrarConta")
            .WithSummary("Encerra a conta (só com saldo zero)")
            .ProducesOperacao();

        return app;
    }

    private static async Task<IResult> AbrirAsync(
        AbrirContaRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken)
    {
        var conta = await barramento.InvokeAsync<ContaDto>(new AbrirConta(requisicao.Titular, requisicao.Moeda), cancellationToken);
        return TypedResults.Created($"/contas/{conta.Id}", conta);
    }

    private static async Task<IResult> ListarAsync(
        int? pagina, int? tamanho, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ResultadoPaginado<ContaDto>>(
            new ListarContas(pagina, tamanho), cancellationToken));

    private static Task<IResult> ObterAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new ObterConta(id), cancellationToken);

    private static Task<IResult> DepositarAsync(
        Guid id, ValorRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new Depositar(id, requisicao.Valor), cancellationToken);

    private static Task<IResult> SacarAsync(
        Guid id, ValorRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new Sacar(id, requisicao.Valor), cancellationToken);

    private static Task<IResult> TransferirAsync(
        Guid id, TransferenciaRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new Transferir(id, requisicao.IbanDestino, requisicao.Valor), cancellationToken);

    private static Task<IResult> BloquearAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new BloquearConta(id), cancellationToken);

    private static Task<IResult> DesbloquearAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new DesbloquearConta(id), cancellationToken);

    private static Task<IResult> EncerrarAsync(Guid id, IMessageBus barramento, CancellationToken cancellationToken) =>
        ExecutarAsync(barramento, new EncerrarConta(id), cancellationToken);

    private static async Task<IResult> ExecutarAsync(
        IMessageBus barramento, object mensagem, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ContaDto>(mensagem, cancellationToken));

    private static RouteHandlerBuilder ProducesOperacao(this RouteHandlerBuilder rota) => rota
        .Produces<ContaDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
