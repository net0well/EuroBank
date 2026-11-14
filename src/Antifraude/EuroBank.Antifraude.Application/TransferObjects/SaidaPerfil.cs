namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Uma transferência que saiu da conta (valor na moeda da conta, sem a tarifa).</summary>
public sealed record SaidaPerfil(Guid TransferenciaId, decimal Valor, string IbanDestino);
