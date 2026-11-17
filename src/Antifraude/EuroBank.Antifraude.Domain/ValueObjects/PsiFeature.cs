using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>O PSI de uma feature (ou do score, com o nome "Score") na janela, contra o treino da versão ativa.</summary>
public sealed record PsiFeature(string Nome, double Psi, NivelDrift Nivel);
