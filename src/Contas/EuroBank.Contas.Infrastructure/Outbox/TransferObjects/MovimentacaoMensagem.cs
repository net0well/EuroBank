namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para DepositoRealizado e SaqueRealizado (valores como string decimal, ex.: "100.00").</summary>
public sealed record MovimentacaoMensagem(
    Guid ContaId, string Valor, string SaldoApos, int Versao, DateTimeOffset OcorridoEm);
