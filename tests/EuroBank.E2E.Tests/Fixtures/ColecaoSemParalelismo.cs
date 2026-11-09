namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>
/// Testes que mexem no laboratório inteiro (ex.: param o Kafka Connect) rodam sozinhos, depois dos outros: com
/// o Connect parado, os comandos das sagas atrasariam e elas reenviariam ou desistiriam por prazo.
/// </summary>
[CollectionDefinition(nameof(ColecaoSemParalelismo), DisableParallelization = true)]
public sealed class ColecaoSemParalelismo;
