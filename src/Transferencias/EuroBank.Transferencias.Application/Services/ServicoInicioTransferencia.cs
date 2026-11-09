using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// Começa a saga quando chega a hora: a API usa ao receber um pedido para já, a varredura do worker usa para
/// as agendadas. Precisa rodar dentro de um IExecutorTransacional: a trava da conta vale até o commit, então
/// duas transferências da mesma conta nunca conferem o limite ao mesmo tempo.
/// </summary>
public sealed class ServicoInicioTransferencia(IRepositorioTransferencias repositorio, PoliticaSaga politica)
{
    public async Task<ResultadoPasso> IniciarSeNaHoraAsync(
        Transferencia transferencia,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        if (!transferencia.EstaNaHora(agora))
        {
            return ResultadoPasso.Ignorado;
        }

        await repositorio.TravarLimiteDaContaAsync(transferencia.ContaOrigemId, cancellationToken);
        var dia = DateOnly.FromDateTime(transferencia.ExecutarEmEfetivo.UtcDateTime);
        var jaTransferido = await repositorio.SomarConsumoDoLimiteAsync(
            transferencia.ContaOrigemId, dia, transferencia.Id, cancellationToken);

        return transferencia.Iniciar(jaTransferido, politica, agora);
    }
}
