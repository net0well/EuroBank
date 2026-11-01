using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.Queries;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Repositories;

namespace EuroBank.Contas.Application.Handlers;

public sealed class ObterContaManipulador(IRepositorioContas repositorio)
{
    public async Task<ContaDto> Handle(ObterConta consulta, CancellationToken cancellationToken) =>
        (await repositorio.ObterOuFalharAsync(consulta.ContaId, cancellationToken)).ParaDto();
}
