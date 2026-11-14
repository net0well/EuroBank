using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Extensions;

public static class RegistroPerfilExtensions
{
    public static void RegistrarPerfil(this ILogger logger, bool aplicado, string evento, Guid contaId, int versao)
    {
        if (aplicado)
        {
            logger.LogDebug("Perfil da conta {ContaId}: {Evento} versão {Versao} aplicado", contaId, evento, versao);
        }
        else
        {
            logger.LogInformation(
                "Perfil da conta {ContaId}: {Evento} versão {Versao} ignorado (versão já aplicada)", contaId, evento, versao);
        }
    }
}
