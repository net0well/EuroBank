namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// A análise como o analista a vê: a operação, a decisão do motor, o sinal de cada regra, as features e, com modelo
/// carregado, a pontuação e a explicação da versão ativa e do challenger. Features nulas nas análises anteriores a elas.
/// </summary>
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
    IReadOnlyList<SinalRegraDto> Sinais,
    IReadOnlyDictionary<string, double>? Features,
    PontuacaoModeloDto? Modelo,
    PontuacaoModeloDto? Challenger);
