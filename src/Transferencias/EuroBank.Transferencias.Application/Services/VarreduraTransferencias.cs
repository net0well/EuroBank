using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// O relógio da saga: começa as agendadas que chegaram na hora, chama o antifraude nos passos de risco e trata os
/// prazos vencidos (reenvio, consulta, desistência). Cada transferência avança na própria transação, travada com
/// FOR UPDATE SKIP LOCKED: várias instâncias do worker dividem o trabalho sem pegar a mesma linha.
/// </summary>
/// <remarks>
/// A chamada ao antifraude acontece com a linha travada: ninguém mais avança a saga enquanto ela espera a resposta,
/// e o deadline curto da chamada limita quanto tempo a trava dura.
/// </remarks>
public sealed class VarreduraTransferencias(
    IRepositorioTransferencias repositorio,
    ServicoInicioTransferencia inicio,
    IAvaliacaoRisco avaliacaoRisco,
    IExecutorTransacional executor,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    PoliticaSaga politica,
    TimeProvider relogio,
    ILogger<VarreduraTransferencias> logger)
{
    public Task<IReadOnlyList<TransferenciaId>> ListarPendentesAsync(int limite, CancellationToken cancellationToken) =>
        repositorio.ListarParaAvancarAsync(relogio.GetUtcNow(), limite, cancellationToken);

    /// <summary>Avança uma transferência. Ignorado se outra instância a travou ou se ela não está mais pendente.</summary>
    public async Task<ResultadoPasso> AvancarAsync(TransferenciaId id, CancellationToken cancellationToken)
    {
        var resultado = ResultadoPasso.Ignorado;
        await executor.ExecutarAsync(
            async token =>
            {
                var agora = relogio.GetUtcNow();
                if (!await repositorio.TravarParaAvancarAsync(id, agora, token))
                {
                    return;
                }

                var transferencia = await repositorio.ObterPorIdAsync(id, token)
                    ?? throw new InvalidOperationException($"Transferência {id} travada mas não encontrada.");

                resultado = transferencia switch
                {
                    { Estado: EstadoTransferencia.Agendada } => await inicio.IniciarSeNaHoraAsync(transferencia, agora, token),
                    { AguardaOAntifraude: true } => await ConsultarAntifraudeAsync(transferencia, agora, token),
                    _ => transferencia.VerificarPrazo(politica, agora),
                };

                await unidadeDeTrabalho.SalvarAsync(token);
                logger.LogInformation(
                    "Varredura da transferência {TransferenciaId}: {Resultado}, estado {Estado} (tentativa {Tentativa})",
                    id, resultado, transferencia.Estado, transferencia.TentativasNoPasso);
            },
            cancellationToken);

        return resultado;
    }

    /// <summary>O instante depois da chamada decide o prazo da análise: a chamada pode ter levado o deadline inteiro.</summary>
    private async Task<ResultadoPasso> ConsultarAntifraudeAsync(Transferencia transferencia, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        try
        {
            var risco = await avaliacaoRisco.AvaliarAsync(transferencia, agora, cancellationToken);
            return transferencia.RegistrarResultadoRisco(risco, politica, relogio.GetUtcNow());
        }
        catch (ExcecaoAntifraudeIndisponivel erro)
        {
            logger.LogWarning(
                erro.InnerException,
                "Antifraude indisponível para a transferência {TransferenciaId}: {Status}",
                transferencia.Id,
                erro.Message);
            return transferencia.RegistrarAntifraudeIndisponivel(erro.Message, politica, relogio.GetUtcNow());
        }
    }
}
