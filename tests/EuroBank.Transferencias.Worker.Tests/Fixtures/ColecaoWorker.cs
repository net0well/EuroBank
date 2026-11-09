namespace EuroBank.Transferencias.Worker.Tests.Fixtures;

/// <summary>As classes de teste do worker compartilham o relógio falso: rodam uma de cada vez.</summary>
[CollectionDefinition(nameof(ColecaoWorker))]
public sealed class ColecaoWorker;
