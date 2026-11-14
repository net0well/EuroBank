namespace EuroBank.Antifraude.Infrastructure.Outbox.TransferObjects;

/// <summary>Evento CasoAberto (eurobank.antifraude.eventos, chave = id da operação): um caso entrou na fila.</summary>
public sealed record CasoAbertoMensagem(
    Guid CasoId,
    Guid AnaliseId,
    Guid OperacaoId,
    Guid ContaId,
    int ScoreFinal,
    DateTimeOffset ExpiraEm,
    DateTimeOffset OcorridoEm);
