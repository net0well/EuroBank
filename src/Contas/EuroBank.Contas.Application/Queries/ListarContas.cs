namespace EuroBank.Contas.Application.Queries;

/// <summary>Lista as contas paginadas, das mais recentes para as mais antigas.</summary>
public sealed record ListarContas(int? Pagina, int? Tamanho);
