using EuroBank.Transferencias.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Transferencias.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços da camada de aplicação. Os manipuladores (classes *Manipulador) são descobertos
    /// pelo Wolverine e não precisam de registro.
    /// </summary>
    public static IServiceCollection AdicionarAplicacaoTransferencias(this IServiceCollection services)
    {
        services.AddScoped<ServicoInicioTransferencia>();
        return services;
    }
}
