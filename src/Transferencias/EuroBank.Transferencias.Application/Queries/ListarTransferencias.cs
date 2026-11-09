namespace EuroBank.Transferencias.Application.Queries;

/// <summary>Filtros opcionais por conta, tipo e estado (Status), com paginação.</summary>
public sealed record ListarTransferencias(Guid? ContaOrigemId, string? Tipo, string? Status, int? Pagina, int? Tamanho);
