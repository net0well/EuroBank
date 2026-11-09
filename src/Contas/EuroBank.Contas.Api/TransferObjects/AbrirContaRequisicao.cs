namespace EuroBank.Contas.Api.TransferObjects;

/// <summary>Moeda: EUR (padrão), USD, GBP ou BRL. Não muda depois da abertura.</summary>
public sealed record AbrirContaRequisicao(string? Titular, string? Moeda);
