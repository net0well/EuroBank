namespace EuroBank.Shared.TransferObjects;

/// <summary>Uma página de resultados com o total de itens existentes.</summary>
public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, long Total);
