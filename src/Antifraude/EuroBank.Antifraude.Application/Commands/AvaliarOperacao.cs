using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Commands;

/// <summary>Avaliar (e gravar) a operação; idempotente pelo OperacaoId.</summary>
public sealed record AvaliarOperacao(ContextoOperacao Contexto);
