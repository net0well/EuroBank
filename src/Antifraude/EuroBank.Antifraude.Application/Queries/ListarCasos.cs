using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Application.Queries;

/// <summary>Sem status: só os pendentes (abertos e em análise), os que expiram primeiro no topo.</summary>
public sealed record ListarCasos(EstadoCaso? Status, int? Pagina, int? Tamanho);
