namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>
/// Por que a saga terminou Cancelada, Recusada ou Divergente, ou por que uma Concluida ou Devolvida saiu do caminho
/// feliz (Reconciliada, DevolvidaPeloBanco).
/// </summary>
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

    /// <summary>A conta de origem está negativa por uma reconciliação: novas transferências são recusadas.</summary>
    SaldoNegativo = 13,

    /// <summary>Concluida depois de ficar Divergente: Contas debitou de novo valor e tarifa.</summary>
    Reconciliada = 14,

    /// <summary>Devolvida: o banco externo devolveu o valor depois da conclusão.</summary>
    DevolvidaPeloBanco = 15,

    /// <summary>Divergente: o banco confirmou a liquidação depois de a saga estornar o débito.</summary>
    ConfirmadaAposEstorno = 16,

    /// <summary>Divergente: o banco devolveu o valor, mas Contas não pôde creditar a conta de origem.</summary>
    DevolucaoNaoCreditada = 17,
}
