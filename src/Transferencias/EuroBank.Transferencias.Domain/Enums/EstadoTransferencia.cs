namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>Estados da saga. Concluida, Cancelada e Recusada são finais.</summary>
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
}
