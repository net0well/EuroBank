namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>O par ativo e challenger em memória, trocado de uma vez (uma avaliação nunca vê metade de uma troca).</summary>
public sealed record ModelosEmUso(ModeloCarregado? Ativo, ModeloCarregado? Challenger)
{
    public static ModelosEmUso Nenhum { get; } = new(null, null);
}
