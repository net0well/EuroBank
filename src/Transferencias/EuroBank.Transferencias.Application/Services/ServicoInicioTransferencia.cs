using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// Começa a saga quando chega a hora (a API ao receber um pedido para já, a varredura para as agendadas) e confere
/// o limite diário. Precisa rodar dentro de um IExecutorTransacional: a trava da conta vale até o commit, então duas
/// transferências da mesma conta nunca conferem o limite ao mesmo tempo.
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

        // Com câmbio, o limite só é conferido quando a cotação chega (ainda não se sabe quanto sai da conta).
        var jaTransferido = transferencia.PrecisaDeCambio
            ? Dinheiro.ZeroEm(transferencia.MoedaOrigem)
            : await ConsumoDoDiaAsync(transferencia, cancellationToken);

        return transferencia.Iniciar(jaTransferido, politica, agora);
    }

    /// <summary>Trava a conta até o fim da transação e soma o que as outras transferências dela já usaram no dia.</summary>
    public async Task<Dinheiro> ConsumoDoDiaAsync(Transferencia transferencia, CancellationToken cancellationToken)
    {
        await repositorio.TravarLimiteDaContaAsync(transferencia.ContaOrigemId, cancellationToken);
        var dia = DateOnly.FromDateTime(transferencia.ExecutarEmEfetivo.UtcDateTime);
        return await repositorio.SomarConsumoDoLimiteAsync(
            transferencia.ContaOrigemId, dia, transferencia.Id, transferencia.MoedaOrigem, cancellationToken);
    }
}
