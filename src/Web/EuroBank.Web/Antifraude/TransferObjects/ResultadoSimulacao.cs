namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>O resultado do Simular (JSON do transcoding): decisão no formato do proto, como DECISAO_APROVADA.</summary>
public sealed record ResultadoSimulacao(
    string Decisao,
    int ScoreFinal,
    IReadOnlyList<SinalRegra>? Sinais,
    int VersaoConfiguracao,
    PontuacaoModelo? Modelo,
    PontuacaoModelo? Challenger);
