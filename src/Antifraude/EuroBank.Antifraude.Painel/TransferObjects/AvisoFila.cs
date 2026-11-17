namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>
/// Algo mudou na fila: um caso novo (CasoId preenchido) ou um caso que fechou (Decisao preenchida, por um analista,
/// pela expiração ou pelo cliente). As telas recarregam pela REST; o aviso só diz o que mostrar no alerta.
/// </summary>
public sealed record AvisoFila(Guid OperacaoId, Guid? CasoId, int ScoreFinal, string? Decisao, string? Origem, string? Analista)
{
    public bool CasoNovo => CasoId is not null;
}
