using EuroBank.Antifraude.Application.Options;
using EuroBank.Antifraude.Application.Services;
using EuroBank.Antifraude.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços da camada de aplicação. Os manipuladores (classes *Manipulador) são descobertos pelo
    /// Wolverine e não precisam de registro.
    /// </summary>
    public static IServiceCollection AdicionarAplicacaoAntifraude(this IServiceCollection services)
    {
        services.AddOptions<OpcoesRevisao>().BindConfiguration(OpcoesRevisao.Secao);
        // Fábrica explícita: o container escolheria o construtor que recebe IEnumerable<IAvaliadorRegra> (lista
        // vazia). O pontuador (os modelos carregados) vem da infraestrutura.
        services.AddSingleton(provedor => new MotorDecisao(provedor.GetRequiredService<IPontuadorFraude>()));
        services.AddScoped<AvaliadorOperacoes>();
        return services;
    }
}
