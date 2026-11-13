namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>
/// Estados da saga. Recusada e Devolvida são finais; Concluida e Cancelada também, salvo o que o banco externo ainda
/// pode mandar depois (devolução, confirmação tardia). Divergente espera a reconciliação.
/// </summary>
public enum EstadoTransferencia
{
    /// <summary>Esperando a hora de executar.</summary>
    Agendada = 1,

    /// <summary>Débito pedido a Contas.</summary>
    AguardandoDebito = 2,

    /// <summary>Contas debitou; a saga decide o próximo passo na mesma operação (fica no histórico).</summary>
    Debitada = 3,

    /// <summary>Liquidação pedida ao banco externo.</summary>
    AguardandoLiquidacao = 4,

    /// <summary>O banco não respondeu a tempo: a saga pergunta o status antes de estornar.</summary>
    ConsultandoLiquidacao = 5,

    /// <summary>Compensação pedida a Contas: devolver valor e tarifa.</summary>
    AguardandoEstorno = 6,

    Concluida = 7,

    /// <summary>Terminou sem dinheiro fora da conta: cancelada pelo cliente ou estornada.</summary>
    Cancelada = 8,

    /// <summary>Terminou sem chegar a debitar (limite, saldo, conta bloqueada...).</summary>
    Recusada = 9,

    /// <summary>Moedas diferentes: cotação pedida ao câmbio, antes do débito.</summary>
    Cotando = 10,

    /// <summary>Câmbio interno: crédito pedido a Contas na conta de destino.</summary>
    AguardandoCredito = 11,

    /// <summary>
    /// O dinheiro não fecha e alguém precisa decidir: o banco confirmou a liquidação depois do estorno, ou devolveu o
    /// valor e Contas não pôde creditá-lo. Fica listada até a reconciliação.
    /// </summary>
    Divergente = 12,

    /// <summary>Reconciliação pedida a Contas: debitar de novo valor e tarifa, mesmo sem saldo.</summary>
    Reconciliando = 13,

    /// <summary>O banco externo devolveu a transferência: crédito do valor pedido a Contas na conta de origem.</summary>
    AguardandoCreditoDevolucao = 14,

    /// <summary>Terminou com o valor de volta na conta de origem (a tarifa não volta).</summary>
    Devolvida = 15,
}
