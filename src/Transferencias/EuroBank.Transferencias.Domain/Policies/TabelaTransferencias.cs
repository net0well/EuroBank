using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.Policies;

/// <summary>Tarifa e limite diário por moeda da conta de origem, para as transferências externas e de câmbio.</summary>
public static class TabelaTransferencias
{
    private static readonly Dictionary<Moeda, decimal> Tarifas = new()
    {
        [Moeda.EUR] = 2.50m,
        [Moeda.USD] = 2.70m,
        [Moeda.GBP] = 2.20m,
        [Moeda.BRL] = 15.00m,
    };

    private static readonly Dictionary<Moeda, decimal> Limites = new()
    {
        [Moeda.EUR] = 5_000m,
        [Moeda.USD] = 5_400m,
        [Moeda.GBP] = 4_300m,
        [Moeda.BRL] = 30_000m,
    };

    /// <summary>Cobrada no mesmo débito do valor; o estorno devolve as duas.</summary>
    public static Dinheiro Tarifa(Moeda moeda) => Dinheiro.De(Tarifas[moeda], moeda);

    /// <summary>Soma do valor debitado (sem a tarifa) das transferências da conta no dia UTC da execução.</summary>
    public static Dinheiro LimiteDiario(Moeda moeda) => Dinheiro.De(Limites[moeda], moeda);

    /// <summary>
    /// Estados em que a transferência ocupa o limite: o débito já foi pedido e o dinheiro não vai voltar.
    /// Agendadas e cotando ainda não ocupam (o limite é conferido antes do débito); estornos, cancelamentos e
    /// recusas liberam.
    /// </summary>
    public static IReadOnlyList<EstadoTransferencia> EstadosQueConsomemLimite { get; } =
    [
        EstadoTransferencia.AguardandoDebito,
        EstadoTransferencia.Debitada,
        EstadoTransferencia.AguardandoLiquidacao,
        EstadoTransferencia.ConsultandoLiquidacao,
        EstadoTransferencia.AguardandoCredito,
        EstadoTransferencia.Concluida,
    ];
}
