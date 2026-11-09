using System.Diagnostics;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>
/// Prazo de cada passo da saga e quantos reenvios ele tem antes de seguir o caminho de falha. Vem da
/// configuração, para os testes ponta a ponta usarem prazos curtos.
/// </summary>
public sealed record PoliticaSaga
{
    public static PoliticaSaga Padrao { get; } = new();

    public TimeSpan PrazoDebito { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoLiquidacao { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoConsulta { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan PrazoEstorno { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Reenvios depois do envio original. O estorno não tem limite: a compensação precisa acontecer.</summary>
    public int ReenviosPorPasso { get; init; } = 3;

    public TimeSpan PrazoDe(EstadoTransferencia estado) => estado switch
    {
        EstadoTransferencia.AguardandoDebito => PrazoDebito,
        EstadoTransferencia.AguardandoLiquidacao => PrazoLiquidacao,
        EstadoTransferencia.ConsultandoLiquidacao => PrazoConsulta,
        EstadoTransferencia.AguardandoEstorno => PrazoEstorno,
        _ => throw new UnreachableException($"O estado {estado} não espera resposta."),
    };
}
