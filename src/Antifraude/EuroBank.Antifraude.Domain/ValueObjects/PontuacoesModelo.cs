namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>As pontuações da versão ativa (decide) e do challenger (sombra); nulas quando não há versão ou ela falhou.</summary>
public sealed record PontuacoesModelo(PontuacaoModelo? Ativa, PontuacaoModelo? Challenger)
{
    public static PontuacoesModelo Nenhuma { get; } = new(null, null);
}
