namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Extrato da conta com uma página de lançamentos, do mais recente para o mais antigo.</summary>
public sealed record ExtratoDto(
    Guid ContaId,
    string Iban,
    string Titular,
    string Status,
    decimal SaldoAtual,
    string Moeda,
    int UltimaVersao,
    int Pagina,
    int Tamanho,
    long Total,
    IReadOnlyList<LancamentoDto> Lancamentos);
