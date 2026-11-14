namespace EuroBank.Notificacoes.Worker.Templates;

/// <summary>
/// O motivo da saga (nome do enum em Transferencias) em linguagem de cliente. Motivo que esta tabela não conhece
/// aparece como veio (tolerant reader: um motivo novo não pode impedir o e-mail).
/// </summary>
public static class DescricoesMotivo
{
    private static readonly Dictionary<string, string> Descricoes = new(StringComparer.Ordinal)
    {
        ["RecusadaPeloBanco"] = "o banco do beneficiário recusou a transferência",
        ["SemRespostaDoBanco"] = "o banco do beneficiário não respondeu a tempo",
        ["CanceladaPeloCliente"] = "você pediu o cancelamento",
        ["SaldoInsuficiente"] = "saldo insuficiente",
        ["ContaBloqueada"] = "a conta de origem está bloqueada",
        ["ContaEncerrada"] = "a conta de origem está encerrada",
        ["ContaInexistente"] = "a conta de origem não existe",
        ["LimiteDiarioExcedido"] = "o limite diário de transferências foi atingido",
        ["SemRespostaDeContas"] = "o sistema de contas não respondeu a tempo",
        ["RecusadaPorContas"] = "a conta de origem recusou o débito",
        ["CambioIndisponivel"] = "não foi possível cotar o câmbio entre as moedas",
        ["DestinoEncerrado"] = "a conta de destino está encerrada",
        ["SaldoNegativo"] = "a conta de origem está com saldo negativo",
        ["ConfirmadaAposEstorno"] = "o banco do beneficiário confirmou a transferência depois do estorno",
        ["DevolucaoNaoCreditada"] = "a devolução não pôde ser creditada na sua conta",
    };

    public static string Descrever(string motivo) => Descricoes.GetValueOrDefault(motivo, motivo);
}
