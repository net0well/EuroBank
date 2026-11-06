using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Application.Interfaces;

public interface IConsultaExtratos
{
    /// <returns>O extrato com a página pedida, ou null se a conta não tem extrato.</returns>
    Task<ExtratoDto?> ObterAsync(Guid contaId, Paginacao paginacao, CancellationToken cancellationToken);
}
