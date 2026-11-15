namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>Medido: há PSI de cada feature. AmostraInsuficiente: poucas análises na janela para um PSI confiável.</summary>
public enum SituacaoDrift
{
    Medido = 1,
    AmostraInsuficiente = 2,
}
