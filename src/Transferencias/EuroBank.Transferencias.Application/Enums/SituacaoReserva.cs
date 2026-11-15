namespace EuroBank.Transferencias.Application.Enums;

/// <summary>Onde está uma Idempotency-Key no Redis quando um pedido chega com ela.</summary>
public enum SituacaoReserva
{
    /// <summary>A key estava livre e agora é deste pedido.</summary>
    Reservada = 1,

    /// <summary>Outro pedido com a mesma key ainda está sendo gravado.</summary>
    EmAndamento = 2,

    /// <summary>A transferência desta key já foi gravada.</summary>
    Concluida = 3,

    /// <summary>Nenhum pedido com esta key no Redis (só numa consulta, sem reservar).</summary>
    Livre = 4,

    /// <summary>Redis fora do ar: vale só a constraint única do Postgres.</summary>
    Indisponivel = 5,
}
