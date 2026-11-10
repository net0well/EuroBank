namespace EuroBank.Contas.Domain.Enums;

public enum StatusTransferenciaDaConta
{
    /// <summary>Debitada e aguardando o fim da saga: trava o encerramento da conta.</summary>
    Pendente = 1,

    /// <summary>Saga concluída: a trava foi liberada.</summary>
    Liberada = 2,

    /// <summary>Compensada: valor e tarifa voltaram para a conta.</summary>
    Estornada = 3,

    /// <summary>Crédito recebido de uma saga de câmbio: só registra o id, para não creditar duas vezes.</summary>
    Recebida = 4,

    /// <summary>Estornada e depois debitada de novo pela reconciliação (o banco liquidou mesmo assim).</summary>
    Reconciliada = 5,

    /// <summary>Concluída e depois devolvida pelo banco externo: o valor voltou para a conta.</summary>
    Devolvida = 6,
}
