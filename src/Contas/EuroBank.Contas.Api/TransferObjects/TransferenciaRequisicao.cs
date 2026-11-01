namespace EuroBank.Contas.Api.TransferObjects;

public sealed record TransferenciaRequisicao(string? IbanDestino, decimal Valor);
