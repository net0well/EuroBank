using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Interfaces;

/// <summary>
/// O perfil de comportamento das contas (no Redis): derivado dos eventos de Contas e reconstruível reprocessando o
/// tópico. Cada atualização só vale se a versão da conta for maior que a última aplicada.
/// </summary>
public interface IPerfisContas
{
    /// <summary>O perfil como as regras o usam; conta nunca vista: perfil vazio (avaliada como nova).</summary>
    Task<PerfilConta> ObterAsync(Guid contaId, DateTimeOffset agora, CancellationToken cancellationToken);

    /// <summary>Aplica a atualização de forma atômica; false quando a versão já foi aplicada (repetido ou atrasado).</summary>
    Task<bool> AplicarAsync(AtualizacaoPerfil atualizacao, CancellationToken cancellationToken);
}
