using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>
/// A decisão do analista (ou a expiração do caso) chega por este evento, sem esperar a próxima consulta da varredura.
/// A decisão das regras já veio na resposta do gRPC: se a saga não espera mais o antifraude, o evento não muda nada
/// e nem vai para o histórico.
/// </summary>
public sealed class AnaliseDecididaManipulador(
    AplicadorRespostas aplicador,
    PoliticaSaga politica,
    ILogger<AnaliseDecididaManipulador> logger)
{
    public Task Handle(EventoAnaliseDecidida evento, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DecisaoRisco>(evento.Decisao, ignoreCase: false, out var decisao) || !Enum.IsDefined(decisao))
        {
            logger.LogWarning(
                "AnaliseDecidida da operação {OperacaoId} com decisão desconhecida {Decisao} ignorada", evento.OperacaoId, evento.Decisao);
            return Task.CompletedTask;
        }

        var resultado = new ResultadoRisco(decisao, evento.AnaliseId, CasoId: null);
        return aplicador.AplicarAsync(
            evento.OperacaoId,
            "AnaliseDecidida",
            (transferencia, agora) => transferencia.AguardaOAntifraude
                ? transferencia.RegistrarResultadoRisco(resultado, politica, agora)
                : ResultadoPasso.Ignorado,
            cancellationToken,
            nivelSeDesconhecida: LogLevel.Debug);
    }
}
