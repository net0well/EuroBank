namespace EuroBank.Extrato.Application.Queries;

public sealed record ObterExtrato(Guid ContaId, int? Pagina, int? Tamanho);
