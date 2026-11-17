namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>O PSI de uma feature (ou do score) contra o treino.</summary>
public sealed record PsiFeature(
    string Nome,
    double Psi,
    string Nivel);
