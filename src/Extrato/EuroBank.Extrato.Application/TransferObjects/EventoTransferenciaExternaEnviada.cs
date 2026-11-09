namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Com câmbio, MoedaDestino, ValorDestino e Cotacao dizem o que o destino recebeu.</summary>
public sealed record EventoTransferenciaExternaEnviada(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    string NomeBeneficiario,
    string? Descricao,
    decimal Valor,
    decimal Tarifa,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm,
    string? MoedaDestino = null,
    decimal? ValorDestino = null,
    decimal? Cotacao = null);
