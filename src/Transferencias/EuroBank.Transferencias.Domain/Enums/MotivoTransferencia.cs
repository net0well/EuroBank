namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>Por que a saga terminou Cancelada ou Recusada.</summary>
public enum MotivoTransferencia
{
    RecusadaPeloBanco = 1,
    SemRespostaDoBanco = 2,
    CanceladaPeloCliente = 3,
    SaldoInsuficiente = 4,
    ContaBloqueada = 5,
    ContaEncerrada = 6,
    ContaInexistente = 7,
    LimiteDiarioExcedido = 8,

    /// <summary>Contas não respondeu ao débito depois dos reenvios.</summary>
    SemRespostaDeContas = 9,

    /// <summary>Contas recusou o débito por um motivo que a saga não conhece.</summary>
    RecusadaPorContas = 10,

    /// <summary>O câmbio não cotou o par ou não respondeu depois dos reenvios.</summary>
    CambioIndisponivel = 11,

    /// <summary>Câmbio interno: a conta de destino recusou o crédito (encerrada).</summary>
    DestinoEncerrado = 12,
}
