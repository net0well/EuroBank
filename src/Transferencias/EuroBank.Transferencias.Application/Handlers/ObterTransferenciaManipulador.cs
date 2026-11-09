using EuroBank.Transferencias.Application.Extensions;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.Queries;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Repositories;

namespace EuroBank.Transferencias.Application.Handlers;

public sealed class ObterTransferenciaManipulador(IRepositorioTransferencias repositorio)
{
    public async Task<TransferenciaDto> Handle(ObterTransferencia consulta, CancellationToken cancellationToken) =>
        (await repositorio.ObterOuFalharAsync(consulta.TransferenciaId, cancellationToken)).ParaDto();
}
