namespace EuroBank.Simuladores.Cambio.Regras;

/// <summary>Taxa já com spread (unidades da moeda de destino por 1 da moeda de origem) e os dois valores.</summary>
public sealed record ResultadoCotacao(bool Disponivel, decimal Taxa, decimal ValorOrigem, decimal ValorDestino, string? Motivo)
{
    public static ResultadoCotacao Indisponivel(string motivo) => new(false, 0m, 0m, 0m, motivo);
}
