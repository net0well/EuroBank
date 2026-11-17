namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>CasoAberto como o painel o lê de eurobank.antifraude.eventos (tolerant reader): um caso entrou na fila.</summary>
public sealed record EventoCasoAberto(Guid CasoId, Guid OperacaoId, int ScoreFinal, DateTimeOffset ExpiraEm, DateTimeOffset OcorridoEm);
