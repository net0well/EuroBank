namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// TransferenciaEnviada, TransferenciaExternaEnviada e TransferenciaCambioEnviada têm os mesmos campos que o perfil
/// usa (tolerant reader: o resto do payload é ignorado). Valor na moeda da conta, sem a tarifa.
/// </summary>
public sealed record EventoSaidaConta(
    Guid ContaId,
    Guid TransferenciaId,
    string IbanDestino,
    decimal Valor,
    decimal SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm);
