namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>As faixas clássicas do PSI: abaixo de 0,1 estável, até 0,25 moderado, acima disso significativo.</summary>
public enum NivelDrift
{
    Estavel = 1,
    Moderado = 2,
    Significativo = 3,
}
