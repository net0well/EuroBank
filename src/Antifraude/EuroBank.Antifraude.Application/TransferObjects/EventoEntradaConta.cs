namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// DepositoRealizado, TransferenciaRecebida e TransferenciaCambioRecebida: dinheiro que entrou na conta, na moeda
/// dela (tolerant reader: o resto do payload é ignorado). As entradas alimentam a feature da mula de dinheiro.
/// </summary>
public sealed record EventoEntradaConta(Guid ContaId, decimal Valor, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm);
