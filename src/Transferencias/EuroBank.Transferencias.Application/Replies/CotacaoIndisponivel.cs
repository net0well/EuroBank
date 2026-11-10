namespace EuroBank.Transferencias.Application.Replies;

public sealed record CotacaoIndisponivel(Guid TransferenciaId, string MoedaOrigem, string MoedaDestino, string Motivo);
