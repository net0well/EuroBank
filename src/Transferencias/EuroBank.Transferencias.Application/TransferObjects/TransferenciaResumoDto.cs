namespace EuroBank.Transferencias.Application.TransferObjects;

/// <summary>Linha da listagem (sem o histórico).</summary>
public sealed record TransferenciaResumoDto(
    Guid Id,
    string Tipo,
    Guid ContaOrigemId,
    string IbanDestino,
    string NomeBeneficiario,
    decimal Valor,
    string MoedaDoValor,
    decimal Tarifa,
    string MoedaOrigem,
    decimal? ValorDestino,
    string MoedaDestino,
    string Estado,
    string? Motivo,
    DateTimeOffset ExecutarEmEfetivo,
    DateTimeOffset AtualizadaEm);
