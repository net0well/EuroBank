namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>Resposta do banco externo a uma consulta de status.</summary>
public enum StatusLiquidacaoExterna
{
    Liquidada = 1,
    NaoLiquidada = 2,
    Recusada = 3,
}
