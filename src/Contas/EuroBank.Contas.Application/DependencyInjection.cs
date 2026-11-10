using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Contas.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços da camada de aplicação. Os manipuladores (classes *Manipulador) são descobertos
    /// pelo Wolverine e não precisam de registro.
    /// </summary>
    public static IServiceCollection AdicionarAplicacaoContas(this IServiceCollection services)
    {
        services.AddSingleton<ServicoTransferencia>();
        services.AddScoped<PassoDaSaga>();
        services.AddScoped<SemeadorContas>();
        return services;
    }
}
