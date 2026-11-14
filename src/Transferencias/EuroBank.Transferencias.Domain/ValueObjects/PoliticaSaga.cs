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

    public TimeSpan PrazoCotacao { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoDebito { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoLiquidacao { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoConsulta { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan PrazoCredito { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoEstorno { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoReconciliacao { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PrazoCreditoDevolucao { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Reenvios depois do envio original. Crédito, estorno, reconciliação e crédito da devolução não têm
    /// limite: desistir deles deixaria dinheiro em dois lugares ou em nenhum.
    /// </summary>
    public int ReenviosPorPasso { get; init; } = 3;

    /// <summary>Liga o passo de risco antes do débito. Desligado, a saga vai direto ao débito, como antes do antifraude.</summary>
    public bool AnaliseRiscoAtiva { get; init; }

    /// <summary>Quanto tempo a saga insiste com o antifraude fora do ar antes de recusar (fail-closed).</summary>
    public TimeSpan PrazoAnaliseRisco { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Espera entre uma tentativa e outra com o antifraude fora do ar.</summary>
    public TimeSpan IntervaloAnaliseRisco { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Em revisão, a decisão chega pelo evento AnaliseDecidida; se ele se perder, a varredura pergunta de novo ao
    /// antifraude a cada intervalo.
    /// </summary>
    public TimeSpan IntervaloConsultaRevisao { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan PrazoDe(EstadoTransferencia estado) => estado switch
    {
        EstadoTransferencia.Cotando => PrazoCotacao,
        EstadoTransferencia.AguardandoDebito => PrazoDebito,
        EstadoTransferencia.AguardandoLiquidacao => PrazoLiquidacao,
        EstadoTransferencia.ConsultandoLiquidacao => PrazoConsulta,
        EstadoTransferencia.AguardandoCredito => PrazoCredito,
        EstadoTransferencia.AguardandoEstorno => PrazoEstorno,
        EstadoTransferencia.Reconciliando => PrazoReconciliacao,
        EstadoTransferencia.AguardandoCreditoDevolucao => PrazoCreditoDevolucao,
        _ => throw new UnreachableException($"O estado {estado} não espera resposta."),
    };
}
