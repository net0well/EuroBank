using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Application.Interfaces;

/// <summary>Leituras do analista, direto do banco (sem passar pelos agregados).</summary>
public interface IConsultaAntifraude
{
    /// <summary>Sem status: os pendentes (abertos e em análise), os que expiram primeiro no topo.</summary>
    Task<ResultadoPaginado<CasoResumoDto>> ListarCasosAsync(EstadoCaso? status, Paginacao paginacao, CancellationToken cancellationToken);

    Task<CasoDetalheDto?> ObterCasoAsync(Guid casoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IbanBloqueadoDto>> ListarIbansBloqueadosAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PaisRiscoDto>> ListarPaisesRiscoAsync(CancellationToken cancellationToken);

    Task<VisaoGeralDto> VisaoGeralAsync(DateTimeOffset desde, CancellationToken cancellationToken);
}
