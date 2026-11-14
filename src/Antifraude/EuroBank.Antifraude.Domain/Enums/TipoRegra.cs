namespace EuroBank.Antifraude.Domain.Enums;

public enum TipoRegra
{
    /// <summary>Decide sozinha (recusa ou revisão), sem pontos.</summary>
    Dura = 1,

    /// <summary>Soma o peso ao score quando dispara.</summary>
    Peso = 2,
}
