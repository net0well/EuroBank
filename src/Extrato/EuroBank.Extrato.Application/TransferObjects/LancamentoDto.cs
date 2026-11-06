namespace EuroBank.Extrato.Application.TransferObjects;

public sealed record LancamentoDto(
    string Tipo, decimal Valor, decimal SaldoApos, int Versao, DateTimeOffset OcorridoEm, string Descricao);
