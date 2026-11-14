namespace EuroBank.Antifraude.Domain.Enums;

public enum EstadoCaso
{
    /// <summary>Na fila, esperando um analista.</summary>
    Aberto = 1,

    /// <summary>Um analista assumiu (o nome fica no caso).</summary>
    EmAnalise = 2,

    Aprovado = 3,
    Recusado = 4,

    /// <summary>Ninguém decidiu dentro do prazo.</summary>
    Expirado = 5,

    /// <summary>O cliente cancelou a transferência antes da decisão.</summary>
    CanceladoPeloCliente = 6,
}
