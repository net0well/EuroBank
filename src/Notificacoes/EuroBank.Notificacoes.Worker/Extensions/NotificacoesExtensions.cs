using EuroBank.Notificacoes.Worker.Options;
using EuroBank.Notificacoes.Worker.Persistence;
using EuroBank.Notificacoes.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EuroBank.Notificacoes.Worker.Extensions;

public static class NotificacoesExtensions
{
    public static IServiceCollection AdicionarNotificacoes(this IServiceCollection services, IConfiguration configuracao)
    {
        services.Configure<OpcoesSmtp>(configuracao.GetSection(OpcoesSmtp.Secao));

        // A connection string é lida quando o DbContext é criado, para os testes poderem sobrescrevê-la.
        services.AddDbContext<NotificacoesDbContext>((provedor, opcoes) => opcoes
            .UseNpgsql(provedor.GetRequiredService<IConfiguration>().GetConnectionString("Notificacoes")
                ?? throw new InvalidOperationException("ConnectionStrings:Notificacoes não configurada.")));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IEnviadorEmail, EnviadorEmailSmtp>();
        services.AddScoped<ServicoNotificacoes>();
        return services;
    }
}
