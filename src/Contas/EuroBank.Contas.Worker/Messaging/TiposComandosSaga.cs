using EuroBank.Contas.Application.Commands;

namespace EuroBank.Contas.Worker.Messaging;

/// <summary>Comandos que o orquestrador de Transferencias manda em eurobank.contas.comandos (header "tipo").</summary>
public static class TiposComandosSaga
{
    public const string Topico = "eurobank.contas.comandos";

    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["DebitarTransferenciaExterna"] = typeof(DebitarTransferenciaExterna),
        ["EstornarTransferencia"] = typeof(EstornarTransferencia),
        ["LiberarTravaTransferencia"] = typeof(LiberarTravaTransferencia),
        ["DebitarTransferenciaCambio"] = typeof(DebitarTransferenciaCambio),
        ["CreditarTransferenciaCambio"] = typeof(CreditarTransferenciaCambio),
    };
}
