namespace EuroBank.Contas.Api.TransferObjects;

/// <summary>Corpo de depósito e saque. Valor em EUR, maior que zero e com até duas casas.</summary>
public sealed record ValorRequisicao(decimal Valor);
