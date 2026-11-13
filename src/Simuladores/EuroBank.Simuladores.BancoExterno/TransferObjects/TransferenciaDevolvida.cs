namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

/// <summary>O banco de destino devolveu uma transferência já liquidada (ex.: conta do beneficiário encerrada).</summary>
public sealed record TransferenciaDevolvida(Guid TransferenciaId, decimal Valor, string Moeda, string Motivo) : IRespostaBancoExterno;
