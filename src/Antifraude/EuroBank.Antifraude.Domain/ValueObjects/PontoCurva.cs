namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O modelo num limiar de probabilidade: precisão (das sinalizadas, quantas eram fraude), recall (das fraudes,
/// quantas foram sinalizadas) e a fração das operações que ficaria acima do limiar.
/// </summary>
public sealed record PontoCurva(double Limiar, double Precisao, double Recall, double TaxaSinalizada);
