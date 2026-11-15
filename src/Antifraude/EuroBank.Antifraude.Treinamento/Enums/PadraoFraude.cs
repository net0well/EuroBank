namespace EuroBank.Antifraude.Treinamento.Enums;

/// <summary>O padrão que gerou uma transferência sintética; Nenhum é o comportamento normal do cliente.</summary>
public enum PadraoFraude
{
    Nenhum = 0,

    /// <summary>Alguém entra na conta e a esvazia para um IBAN novo, de madrugada.</summary>
    TomadaDeConta = 1,

    /// <summary>Conta nova que recebe dinheiro de várias origens e o repassa rápido.</summary>
    MulaDeDinheiro = 2,

    /// <summary>Vários valores pequenos em sequência, para ver se a conta responde.</summary>
    TesteDeCartao = 3,

    /// <summary>O próprio cliente, convencido por um golpista, manda um valor fora do padrão para um IBAN novo.</summary>
    EngenhariaSocial = 4,
}
