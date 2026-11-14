namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>
/// Testes que mudam regras, limiares ou listas (valem para todas as avaliações) rodam sozinhos, depois dos outros, e
/// devolvem a configuração ao estado inicial.
/// </summary>
[CollectionDefinition(nameof(ColecaoConfiguracaoGlobal), DisableParallelization = true)]
public sealed class ColecaoConfiguracaoGlobal;
