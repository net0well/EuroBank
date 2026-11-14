using EuroBank.Antifraude.Grpc;
using Google.Protobuf.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EuroBank.Antifraude.Api.OpenApi;

/// <summary>
/// Põe no Swagger as rotas REST que o JSON transcoding cria a partir do .proto (o Swashbuckle não as enxerga). As
/// rotas repetem as anotações google.api.http do antifraude.proto; os corpos vêm dos descritores.
/// </summary>
public sealed class RotasTranscodingFilter : IDocumentFilter
{
    private const string Grupo = "Avaliações (gRPC JSON transcoding)";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths ??= new OpenApiPaths();
        swaggerDoc.Paths["/v1/avaliacoes"] = Caminho(HttpMethod.Post, Operacao(
            swaggerDoc,
            "Avalia a operação (idempotente pelo operacaoId)",
            corpo: SolicitacaoAvaliacao.Descriptor));
        swaggerDoc.Paths["/v1/avaliacoes/{analiseId}"] = Caminho(HttpMethod.Get, Operacao(
            swaggerDoc,
            "Consulta uma análise",
            corpo: null,
            parametro: "analiseId"));
        swaggerDoc.Paths["/v1/simulacoes"] = Caminho(HttpMethod.Post, Operacao(
            swaggerDoc,
            "Avaliação hipotética: não grava análise nem abre caso",
            corpo: SolicitacaoAvaliacao.Descriptor));
    }

    private static OpenApiPathItem Caminho(HttpMethod metodo, OpenApiOperation operacao) =>
        new() { Operations = new Dictionary<HttpMethod, OpenApiOperation> { [metodo] = operacao } };

    private static OpenApiOperation Operacao(OpenApiDocument documento, string resumo, MessageDescriptor? corpo, string? parametro = null)
    {
        var operacao = new OpenApiOperation
        {
            Summary = resumo,
            Tags = new HashSet<OpenApiTagReference> { new(Grupo, documento) },
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "Resultado da avaliação",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new() { Schema = EsquemaProtobuf.Gerar(ResultadoAvaliacao.Descriptor) },
                    },
                },
                ["400"] = new OpenApiResponse { Description = "Pedido inválido (InvalidArgument)" },
                ["401"] = new OpenApiResponse { Description = "API key ausente ou inválida" },
                ["503"] = new OpenApiResponse { Description = "Antifraude indisponível (Unavailable)" },
            },
        };

        if (corpo is not null)
        {
            operacao.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new() { Schema = EsquemaProtobuf.Gerar(corpo) },
                },
            };
        }

        if (parametro is not null)
        {
            operacao.Parameters =
            [
                new OpenApiParameter
                {
                    Name = parametro,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
                },
            ];
        }

        return operacao;
    }
}
