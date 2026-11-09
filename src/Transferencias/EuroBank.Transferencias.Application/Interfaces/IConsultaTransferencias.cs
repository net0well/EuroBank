using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.TransferObjects;

namespace EuroBank.Transferencias.Application.Interfaces;

/// <summary>Consultas de leitura (sem rastreamento de mudanças) sobre as transferências.</summary>
public interface IConsultaTransferencias
{
    Task<ResultadoPaginado<TransferenciaResumoDto>> ListarAsync(
        FiltroTransferencias filtro,
        Paginacao paginacao,
        CancellationToken cancellationToken);
}
