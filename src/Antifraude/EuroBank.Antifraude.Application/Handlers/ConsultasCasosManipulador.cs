using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Leituras do analista sobre os casos.</summary>
public sealed class ConsultasCasosManipulador(IConsultaAntifraude consulta)
{
    public Task<ResultadoPaginado<CasoResumoDto>> Handle(ListarCasos consultaCasos, CancellationToken cancellationToken) =>
        consulta.ListarCasosAsync(consultaCasos.Status, Paginacao.Criar(consultaCasos.Pagina, consultaCasos.Tamanho), cancellationToken);

    public async Task<CasoDetalheDto> Handle(ObterCaso consultaCaso, CancellationToken cancellationToken) =>
        await consulta.ObterCasoAsync(consultaCaso.CasoId, cancellationToken) ?? throw new ExcecaoCasoNaoEncontrado(consultaCaso.CasoId);
}
