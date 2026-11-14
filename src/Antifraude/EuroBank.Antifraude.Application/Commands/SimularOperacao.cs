using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Commands;

/// <summary>Avaliação hipotética: as mesmas regras e o mesmo perfil, sem gravar análise nem abrir caso.</summary>
public sealed record SimularOperacao(ContextoOperacao Contexto);
