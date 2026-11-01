namespace EuroBank.Contas.Infrastructure.Outbox.TransferObjects;

/// <summary>Payload publicado para ContaBloqueada, ContaDesbloqueada e ContaEncerrada.</summary>
public sealed record AlteracaoStatusMensagem(
    Guid ContaId, int Versao, DateTimeOffset OcorridoEm);
