namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>A análise: a operação, os sinais, as features e o que o modelo disse.</summary>
public sealed record AnaliseDetalhe(
    Guid Id,
    Guid OperacaoId,
    string TipoOperacao,
    Guid ContaId,
    decimal Valor,
    string Moeda,
    string? IbanDestino,
    DateTimeOffset Momento,
    string? DispositivoId,
    string? Localizacao,
    string Decisao,
    int ScoreFinal,
    int VersaoConfiguracao,
    DateTimeOffset CriadaEm,
    DateTimeOffset? DecididaEm,
    IReadOnlyList<SinalRegra> Sinais,
    IReadOnlyDictionary<string, double>? Features,
    PontuacaoModelo? Modelo,
    PontuacaoModelo? Challenger);
