using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Infrastructure.Messaging;

/// <summary>
/// Eventos de eurobank.contas.eventos que o perfil entende, agrupados pela forma que importa a ele: abertura, saída
/// de dinheiro para um IBAN, outro movimento de saldo, mudança de status. Tipos fora da tabela viram
/// EventoDesconhecido e são ignorados com alerta.
/// </summary>
public static class TiposEventosContas
{
    public const string Topico = "eurobank.contas.eventos";

    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["ContaAberta"] = typeof(EventoContaAberta),
        ["TransferenciaEnviada"] = typeof(EventoSaidaConta),
        ["TransferenciaExternaEnviada"] = typeof(EventoSaidaConta),
        ["TransferenciaCambioEnviada"] = typeof(EventoSaidaConta),
        ["DepositoRealizado"] = typeof(EventoMovimentoConta),
        ["SaqueRealizado"] = typeof(EventoMovimentoConta),
        ["TransferenciaRecebida"] = typeof(EventoMovimentoConta),
        ["TransferenciaCambioRecebida"] = typeof(EventoMovimentoConta),
        ["EstornoTransferencia"] = typeof(EventoMovimentoConta),
        ["DevolucaoTransferenciaExterna"] = typeof(EventoMovimentoConta),
        ["DebitoReconciliacao"] = typeof(EventoMovimentoConta),
        ["ContaBloqueada"] = typeof(EventoStatusConta),
        ["ContaDesbloqueada"] = typeof(EventoStatusConta),
        ["ContaEncerrada"] = typeof(EventoStatusConta),
    };
}
