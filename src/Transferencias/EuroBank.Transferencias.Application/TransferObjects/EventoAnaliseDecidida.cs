namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>
/// AnaliseDecidida como Transferencias o lê de eurobank.antifraude.eventos (tolerant reader). OperacaoId é o id da
/// transferência (ou de um saque, que esta saga não conhece). Decisao: Aprovada, Recusada, Expirada ou Cancelada.
/// </summary>
public sealed record EventoAnaliseDecidida(Guid AnaliseId, Guid OperacaoId, string Decisao, string Origem, DateTimeOffset OcorridoEm);
