namespace EuroBank.Extrato.Worker.Tests.Fixtures;

/// <summary>
/// Os testes do cache mexem na marca global de cache suspeito e apagam chaves do Redis: rodam sozinhos, depois dos
/// outros.
/// </summary>
[CollectionDefinition(nameof(ColecaoCache), DisableParallelization = true)]
public sealed class ColecaoCache;
