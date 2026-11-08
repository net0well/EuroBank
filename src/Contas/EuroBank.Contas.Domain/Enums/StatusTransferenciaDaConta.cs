namespace EuroBank.Contas.Domain.Enums;

public enum StatusTransferenciaDaConta
{
    /// <summary>Debitada e aguardando o fim da saga: trava o encerramento da conta.</summary>
    Pendente = 1,

    /// <summary>Saga concluída: a trava foi liberada.</summary>
    Liberada = 2,

    /// <summary>Compensada: valor e tarifa voltaram para a conta.</summary>
    Estornada = 3,
}
