using EuroBank.Contas.Application.Enums;
using EuroBank.Contas.Application.Exceptions;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Contas.Application.Services;

/// <summary>
/// Pergunta ao antifraude antes do saque e da transferência interna. Só a aprovada segue; recusa e revisão viram 422
/// (o caso fica na fila do analista e vira rótulo do treino, mas este pedido não executa).
/// </summary>
public sealed class ConferenciaAntifraude(IAnaliseRisco antifraude, ILogger<ConferenciaAntifraude> logger)
{
    /// <exception cref="ExcecaoAntifraudeRevisao">Recusada ou em revisão.</exception>
    /// <exception cref="ExcecaoAntifraudeIndisponivel">Sem resposta do antifraude.</exception>
    public async Task GarantirAprovadaAsync(
        TipoOperacaoRisco tipo,
        Guid contaId,
        Dinheiro valor,
        string? ibanDestino,
        string? dispositivoId,
        Localizacao? localizacao,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var operacao = new OperacaoEmAnalise(Guid.CreateVersion7(), tipo, contaId, valor, ibanDestino, agora, dispositivoId, localizacao);
        var resultado = await antifraude.AvaliarAsync(operacao, cancellationToken);
        if (resultado.Decisao == DecisaoRisco.Aprovada)
        {
            return;
        }

        logger.LogInformation(
            "{Tipo} {OperacaoId} da conta {ContaId} não aprovado pelo antifraude: {Decisao} (caso {CasoId})",
            tipo,
            operacao.OperacaoId,
            contaId,
            resultado.Decisao,
            resultado.CasoId);
        throw new ExcecaoAntifraudeRevisao(resultado.Decisao == DecisaoRisco.EmRevisao);
    }
}
