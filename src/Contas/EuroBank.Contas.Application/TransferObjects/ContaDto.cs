namespace EuroBank.Contas.Application.TransferObjects;

/// <summary>Representação da conta devolvida pela API.</summary>
public sealed record ContaDto(
    Guid Id,
    string Iban,
    string Titular,
    decimal Saldo,
    string Moeda,
    string Status,
    int Versao,
    DateTimeOffset AbertaEm);
