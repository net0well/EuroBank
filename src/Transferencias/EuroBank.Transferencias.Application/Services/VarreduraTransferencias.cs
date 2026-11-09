using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// O relógio da saga: começa as agendadas que chegaram na hora e trata os prazos vencidos (reenvio, consulta,
/// desistência). Cada transferência avança na própria transação, travada com FOR UPDATE SKIP LOCKED: várias
/// instâncias do worker dividem o trabalho sem pegar a mesma linha.
/// </summary>
public sealed class VarreduraTransferencias(
    IRepositorioTransferencias repositorio,
    ServicoInicioTransferencia inicio,
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

                resultado = transferencia.Estado == EstadoTransferencia.Agendada
                    ? await inicio.IniciarSeNaHoraAsync(transferencia, agora, token)
                    : transferencia.VerificarPrazo(politica, agora);

                await unidadeDeTrabalho.SalvarAsync(token);
                logger.LogInformation(
                    "Varredura da transferência {TransferenciaId}: {Resultado}, estado {Estado} (tentativa {Tentativa})",
                    id, resultado, transferencia.Estado, transferencia.TentativasNoPasso);
            },
            cancellationToken);

        return resultado;
    }
}
