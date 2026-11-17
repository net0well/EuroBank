namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>O pedido do Simular no JSON do transcoding (tipo como TIPO_OPERACAO_EXTERNA).</summary>
public sealed record SolicitacaoSimulacao(
    string OperacaoId,
    string TipoOperacao,
    string ContaId,
    ValorDecimal Valor,
    string Moeda,
    string? IbanDestino);
