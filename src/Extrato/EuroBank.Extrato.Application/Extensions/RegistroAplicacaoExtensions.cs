using EuroBank.Extrato.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Extensions;

public static class RegistroAplicacaoExtensions
{
    /// <summary>Registra no log, de forma estruturada, o que aconteceu ao aplicar um evento na projeção.</summary>
    public static void RegistrarResultado(
        this ILogger logger,
        ResultadoAplicacao resultado,
        string tipoEvento,
        Guid contaId,
        int versao)
    {
        switch (resultado)
        {
            case ResultadoAplicacao.IgnoradoJaAplicado:
                logger.LogInformation(
                    "Evento {TipoEvento} da conta {ContaId} ignorado: a versão {Versao} já foi aplicada",
                    tipoEvento, contaId, versao);
                break;
            case ResultadoAplicacao.AplicadoComLacuna:
                logger.LogWarning(
                    "Evento {TipoEvento} da conta {ContaId} aplicado com lacuna: faltaram versões antes da {Versao}",
                    tipoEvento, contaId, versao);
                break;
            default:
                logger.LogInformation(
                    "Evento {TipoEvento} da conta {ContaId} aplicado na versão {Versao}",
                    tipoEvento, contaId, versao);
                break;
        }
    }
}
