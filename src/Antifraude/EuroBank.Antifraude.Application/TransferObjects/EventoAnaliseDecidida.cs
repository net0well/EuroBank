using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// AnaliseDecidida como o worker do antifraude o lê do próprio tópico (tolerant reader): só a aprovada ensina o perfil,
/// com o dispositivo e a localização da operação.
/// </summary>
public sealed record EventoAnaliseDecidida(
    Guid AnaliseId,
    Guid ContaId,
    string Decisao,
    DateTimeOffset MomentoOperacao,
    string? DispositivoId,
    Localizacao? Localizacao);
