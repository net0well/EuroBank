using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>Resposta do antifraude: a decisão, a análise que a produziu e o caso de revisão, quando houver.</summary>
public sealed record ResultadoRisco(DecisaoRisco Decisao, Guid AnaliseId, Guid? CasoId);
