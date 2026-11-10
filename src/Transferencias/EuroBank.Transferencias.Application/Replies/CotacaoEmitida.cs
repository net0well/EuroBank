namespace EuroBank.Transferencias.Application.Replies;

/// <summary>Resposta do câmbio (eurobank.cambio.respostas): taxa travada até ValidaAte e os dois valores.</summary>
public sealed record CotacaoEmitida(
    Guid TransferenciaId,
    Guid CotacaoId,
    string MoedaOrigem,
    string MoedaDestino,
    decimal Taxa,
    decimal ValorOrigem,
    decimal ValorDestino,
    DateTimeOffset ValidaAte);
