namespace EuroBank.Simuladores.BancoExterno.Regras;

/// <summary>O que o banco faz e quando responde; AtrasoDevolucao só no final 94 (contado a partir da confirmação).</summary>
public sealed record DecisaoLiquidacao(ComportamentoLiquidacao Comportamento, TimeSpan Atraso, TimeSpan? AtrasoDevolucao = null);
