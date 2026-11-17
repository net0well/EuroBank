namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Precisão, recall e fração sinalizada num limiar.</summary>
public sealed record PontoCurva(
    double Limiar,
    double Precisao,
    double Recall,
    double TaxaSinalizada);
