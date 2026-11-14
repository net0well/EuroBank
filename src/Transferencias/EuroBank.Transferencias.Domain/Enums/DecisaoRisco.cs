namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>A decisão do antifraude sobre a operação, como a saga a enxerga.</summary>
public enum DecisaoRisco
{
    Aprovada = 1,

    /// <summary>Caso aberto na fila do analista.</summary>
    EmRevisao = 2,

    Recusada = 3,

    /// <summary>O caso passou do prazo sem decisão.</summary>
    Expirada = 4,

    /// <summary>O cliente cancelou a transferência durante a análise.</summary>
    Cancelada = 5,
}
