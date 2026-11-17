using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Commands;

/// <summary>
/// Mede o drift da janela que termina em Agora contra a versão ativa e grava a medição; apaga as medições mais
/// antigas que a retenção. Sem versão ativa, não mede. AmostraMaxima limita quantas análises (as mais recentes) entram.
/// </summary>
public sealed record MedirDrift(DateTimeOffset Agora, TimeSpan Janela, LimitesDrift Limites, int AmostraMaxima, TimeSpan Retencao);
