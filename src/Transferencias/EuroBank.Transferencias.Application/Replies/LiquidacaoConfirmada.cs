namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta do banco externo (eurobank.banco-externo.respostas): o dinheiro chegou ao destino.</summary>
public sealed record LiquidacaoConfirmada(Guid TransferenciaId, DateTimeOffset ConfirmadaEm);
