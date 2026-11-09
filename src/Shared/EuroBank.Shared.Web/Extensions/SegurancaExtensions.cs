using EuroBank.Shared.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace EuroBank.Shared.Web.Extensions;

public static class SegurancaExtensions
{
    private const string EsquemaApiKey = "ApiKey";

    /// <summary>Lê a API key da seção Seguranca e falha no startup se ela não estiver configurada.</summary>
    public static IServiceCollection AdicionarApiKey(this IServiceCollection services)
    {
        services.AddOptions<OpcoesSeguranca>()
            .BindConfiguration(OpcoesSeguranca.Secao)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }

    public static IApplicationBuilder UsarApiKey(this IApplicationBuilder app) =>
        app.UseMiddleware<ValidacaoApiKeyMiddleware>();

    /// <summary>Swagger com o esquema de segurança X-Api-Key (botão Authorize na UI).</summary>
    public static IServiceCollection AdicionarSwaggerComApiKey(this IServiceCollection services, string titulo)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new OpenApiInfo { Title = titulo, Version = "v1" });
            opcoes.AddSecurityDefinition(EsquemaApiKey, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = OpcoesSeguranca.HeaderApiKey,
                Description = "API key do laboratório (veja o .env).",
            });
            opcoes.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(EsquemaApiKey, documento)] = [],
            });
        });
        return services;
    }
}
