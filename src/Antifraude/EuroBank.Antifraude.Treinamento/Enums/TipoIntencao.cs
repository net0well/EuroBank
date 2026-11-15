namespace EuroBank.Antifraude.Treinamento.Enums;

/// <summary>O que acontece num instante da simulação de uma conta.</summary>
public enum TipoIntencao
{
    Abertura = 1,

    /// <summary>Salário, depósito ou crédito recebido.</summary>
    Entrada = 2,

    /// <summary>Uma transferência do dia a dia: o beneficiário e o valor são sorteados na hora.</summary>
    SaidaNormal = 3,

    /// <summary>Uma transferência com valor e IBAN já definidos (um passo de um episódio de fraude).</summary>
    Saida = 4,

    /// <summary>O começo de um episódio de fraude, que agenda os próprios passos.</summary>
    Episodio = 5,
}
