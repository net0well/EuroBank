namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Uma linha do log de uma execução de treino.</summary>
public sealed record RegistroTreino(DateTimeOffset Momento, string Mensagem);
