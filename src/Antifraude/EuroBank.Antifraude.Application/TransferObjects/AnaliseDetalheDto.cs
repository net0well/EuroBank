namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>A análise como o analista a vê: a operação, a decisão do motor e o sinal de cada regra.</summary>
public sealed record AnaliseDetalheDto(
    Guid Id,
    Guid OperacaoId,
    string TipoOperacao,
    Guid ContaId,
    decimal Valor,
    string Moeda,
    string? IbanDestino,
    DateTimeOffset Momento,
    string Decisao,
    int ScoreFinal,
    int VersaoConfiguracao,
    DateTimeOffset CriadaEm,
    DateTimeOffset? DecididaEm,
    IReadOnlyList<SinalRegraDto> Sinais);
