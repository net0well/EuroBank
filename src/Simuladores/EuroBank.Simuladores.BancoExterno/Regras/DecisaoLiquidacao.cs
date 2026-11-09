namespace EuroBank.Simuladores.BancoExterno.Regras;

public sealed record DecisaoLiquidacao(ComportamentoLiquidacao Comportamento, TimeSpan Atraso);
