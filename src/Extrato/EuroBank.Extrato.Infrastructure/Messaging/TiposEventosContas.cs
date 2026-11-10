using EuroBank.Extrato.Application.TransferObjects;

namespace EuroBank.Extrato.Infrastructure.Messaging;

/// <summary>
/// Tipos que o Extrato entende em eurobank.contas.eventos (header "tipo") e o DTO de cada um. Tipos fora da
/// tabela viram EventoDesconhecido e são ignorados com alerta.
/// </summary>
public static class TiposEventosContas
{
    public static IReadOnlyDictionary<string, Type> Mapa { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["ContaAberta"] = typeof(EventoContaAberta),
        ["DepositoRealizado"] = typeof(EventoDepositoRealizado),
        ["SaqueRealizado"] = typeof(EventoSaqueRealizado),
        ["TransferenciaEnviada"] = typeof(EventoTransferenciaEnviada),
        ["TransferenciaRecebida"] = typeof(EventoTransferenciaRecebida),
        ["ContaBloqueada"] = typeof(EventoContaBloqueada),
        ["ContaDesbloqueada"] = typeof(EventoContaDesbloqueada),
        ["ContaEncerrada"] = typeof(EventoContaEncerrada),
        ["TransferenciaExternaEnviada"] = typeof(EventoTransferenciaExternaEnviada),
        ["EstornoTransferencia"] = typeof(EventoEstornoTransferencia),
        ["TransferenciaCambioEnviada"] = typeof(EventoTransferenciaCambioEnviada),
        ["TransferenciaCambioRecebida"] = typeof(EventoTransferenciaCambioRecebida),
    };
}
