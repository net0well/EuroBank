using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.Policies;

/// <summary>Tarifa e limite diário das transferências externas (valores em EUR).</summary>
public static class TabelaTransferencias
{
    /// <summary>Cobrada no mesmo débito do valor; o estorno devolve as duas.</summary>
    public static Dinheiro TarifaExterna { get; } = Dinheiro.De(2.50m);

    /// <summary>Soma do valor (sem a tarifa) das transferências da conta no dia UTC da execução.</summary>
    public static Dinheiro LimiteDiario { get; } = Dinheiro.De(5_000m);

    /// <summary>
    /// Estados em que a transferência ocupa o limite: já começou e o dinheiro não vai voltar. Agendadas ainda não
    /// ocupam (o limite é conferido quando começam); estornos, cancelamentos e recusas liberam.
    /// </summary>
    public static IReadOnlyList<EstadoTransferencia> EstadosQueConsomemLimite { get; } =
    [
        EstadoTransferencia.AguardandoDebito,
        EstadoTransferencia.Debitada,
        EstadoTransferencia.AguardandoLiquidacao,
        EstadoTransferencia.ConsultandoLiquidacao,
        EstadoTransferencia.Concluida,
    ];
}
