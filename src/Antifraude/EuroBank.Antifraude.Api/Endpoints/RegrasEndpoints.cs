using EuroBank.Antifraude.Api.TransferObjects;
using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using Wolverine;

namespace EuroBank.Antifraude.Api.Endpoints;

/// <summary>Regras e configuração de decisão: os ajustes valem na próxima avaliação, sem reiniciar.</summary>
public static class RegrasEndpoints
{
    public static IEndpointRouteBuilder MapearEndpointsRegras(this IEndpointRouteBuilder app)
    {
        var regras = app.MapGroup("/regras").WithTags("Regras");

        regras.MapGet("", ListarAsync)
            .WithName("ListarRegras")
            .WithSummary("Todas as regras, com peso, ativação, modo sombra e parâmetros")
            .Produces<IReadOnlyList<RegraDto>>();

        regras.MapGet("/{codigo}", ObterAsync)
            .WithName("ObterRegra")
            .Produces<RegraDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        regras.MapPut("/{codigo}", AjustarAsync)
            .WithName("AjustarRegra")
            .WithSummary("Peso (0 a 100; 0 nas duras), ativa, sombra e parâmetros (só os que mudam)")
            .Produces<RegraDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var configuracao = app.MapGroup("/configuracao-decisao").WithTags("Regras");

        configuracao.MapGet("", ObterConfiguracaoAsync)
            .WithName("ObterConfiguracaoDecisao")
            .WithSummary("Limiares do score e peso do modelo (a versão atual)")
            .Produces<ConfiguracaoDecisaoDto>();

        configuracao.MapPut("", AlterarConfiguracaoAsync)
            .WithName("AlterarConfiguracaoDecisao")
            .WithSummary("Cria uma nova versão: abaixo do limiar de revisão aprova, a partir do de recusa recusa")
            .Produces<ConfiguracaoDecisaoDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListarAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<IReadOnlyList<RegraDto>>(new ListarRegras(), cancellationToken));

    private static async Task<IResult> ObterAsync(string codigo, IMessageBus barramento, CancellationToken cancellationToken)
    {
        var regras = await barramento.InvokeAsync<IReadOnlyList<RegraDto>>(new ListarRegras(), cancellationToken);
        var codigoRegra = Codigo(codigo).ToString();
        return TypedResults.Ok(regras.Single(regra => regra.Codigo == codigoRegra));
    }

    private static async Task<IResult> AjustarAsync(
        string codigo, AjusteRegraRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<RegraDto>(
            new AjustarRegra(Codigo(codigo), requisicao.Peso, requisicao.Ativa, requisicao.Sombra, requisicao.Parametros),
            cancellationToken));

    private static async Task<IResult> ObterConfiguracaoAsync(IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ConfiguracaoDecisaoDto>(new ObterConfiguracao(), cancellationToken));

    private static async Task<IResult> AlterarConfiguracaoAsync(
        ConfiguracaoRequisicao requisicao, IMessageBus barramento, CancellationToken cancellationToken) =>
        TypedResults.Ok(await barramento.InvokeAsync<ConfiguracaoDecisaoDto>(
            new AlterarConfiguracao(requisicao.LimiarRevisao, requisicao.LimiarRecusa, requisicao.PesoModelo), cancellationToken));

    private static CodigoRegra Codigo(string codigo) =>
        Enum.TryParse<CodigoRegra>(codigo, ignoreCase: true, out var valor) && Enum.IsDefined(valor)
            ? valor
            : throw new ExcecaoRegraNaoEncontrada(codigo);
}
