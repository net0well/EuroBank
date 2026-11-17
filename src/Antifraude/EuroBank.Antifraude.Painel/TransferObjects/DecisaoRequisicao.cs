namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Decidir um caso: Aprovar ou Recusar, com justificativa.</summary>
public sealed record DecisaoRequisicao(
    string Analista,
    string Decisao,
    string Justificativa,
    bool BloquearIban);
