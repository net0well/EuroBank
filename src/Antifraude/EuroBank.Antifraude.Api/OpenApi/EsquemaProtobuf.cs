using System.Text.Json.Nodes;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.OpenApi;

namespace EuroBank.Antifraude.Api.OpenApi;

/// <summary>
/// Esquema JSON de uma mensagem protobuf, lido do descritor gerado a partir do .proto e seguindo o mapeamento JSON
/// do protobuf (nomes em camelCase, enums como texto, Timestamp como data-hora). Mudou o .proto, mudou o Swagger.
/// </summary>
public static class EsquemaProtobuf
{
    public static OpenApiSchema Gerar(MessageDescriptor mensagem)
    {
        if (mensagem.FullName == Timestamp.Descriptor.FullName)
        {
            return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        }

        var propriedades = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        foreach (var campo in mensagem.Fields.InFieldNumberOrder())
        {
            var esquema = Campo(campo);
            propriedades[campo.JsonName] = campo.IsRepeated ? new OpenApiSchema { Type = JsonSchemaType.Array, Items = esquema } : esquema;
        }

        return new OpenApiSchema { Type = JsonSchemaType.Object, Properties = propriedades };
    }

    private static OpenApiSchema Campo(FieldDescriptor campo) => campo.FieldType switch
    {
        FieldType.String => new OpenApiSchema { Type = JsonSchemaType.String },
        FieldType.Bool => new OpenApiSchema { Type = JsonSchemaType.Boolean },
        FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 or FieldType.UInt32 or FieldType.Fixed32 =>
            new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 or FieldType.UInt64 or FieldType.Fixed64 =>
            new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" },
        FieldType.Double or FieldType.Float => new OpenApiSchema { Type = JsonSchemaType.Number },
        FieldType.Enum => new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = campo.EnumType.Values.Select(valor => (JsonNode)JsonValue.Create(valor.Name)!).ToList(),
        },
        FieldType.Message => Gerar(campo.MessageType),
        _ => new OpenApiSchema { Type = JsonSchemaType.String },
    };
}
