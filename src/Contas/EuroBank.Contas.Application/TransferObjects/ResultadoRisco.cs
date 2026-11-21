using EuroBank.Contas.Application.Enums;

namespace EuroBank.Contas.Application.TransferObjects;

/// <summary>A decisão do antifraude, com o caso quando a operação foi para a revisão.</summary>
public sealed record ResultadoRisco(DecisaoRisco Decisao, Guid? AnaliseId, Guid? CasoId)
{
    public static ResultadoRisco SemAnalise { get; } = new(DecisaoRisco.Aprovada, null, null);
}
