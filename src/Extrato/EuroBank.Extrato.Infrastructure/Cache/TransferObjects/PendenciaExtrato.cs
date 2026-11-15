namespace EuroBank.Extrato.Infrastructure.Cache.TransferObjects;

/// <summary>
/// O que o write-behind ainda deve ao Postgres por um evento aplicado: o estado do extrato depois dele e os
/// lançamentos novos. Descarregar duas vezes não muda nada (o estado só avança com versão maior; o lançamento é único
/// por conta e versão).
/// </summary>
public sealed record PendenciaExtrato(EstadoExtrato Estado, IReadOnlyList<LancamentoGravado> Lancamentos);
