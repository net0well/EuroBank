namespace EuroBank.Simuladores.BancoExterno.Regras;

public enum ComportamentoLiquidacao
{
    /// <summary>Liquida e responde LiquidacaoConfirmada.</summary>
    Confirmar = 1,

    /// <summary>Recusa e responde LiquidacaoRecusada.</summary>
    Recusar = 2,

    /// <summary>Não liquida e nunca responde, nem às consultas (final 98).</summary>
    Silenciar = 3,

    /// <summary>Liquida, mas a resposta se perde; a consulta diz Liquidada (final 96).</summary>
    LiquidarSemResponder = 4,
}
