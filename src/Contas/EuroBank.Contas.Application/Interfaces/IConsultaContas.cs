using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Interfaces;

/// <summary>Consultas de leitura (sem rastreamento de mudanças) sobre as contas.</summary>
public interface IConsultaContas
{
    Task<ResultadoPaginado<ContaDto>> ListarAsync(Paginacao paginacao, CancellationToken cancellationToken);
}
