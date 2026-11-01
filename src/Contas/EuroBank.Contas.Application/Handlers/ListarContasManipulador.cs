using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Queries;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class ListarContasManipulador(IConsultaContas consultaContas)
{
    public Task<ResultadoPaginado<ContaDto>> Handle(ListarContas consulta, CancellationToken cancellationToken) =>
        consultaContas.ListarAsync(Paginacao.Criar(consulta.Pagina, consulta.Tamanho), cancellationToken);
}
