using EuroBank.Extrato.Application.Interfaces;
using EuroBank.Extrato.Application.Queries;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class ObterExtratoManipulador(IConsultaExtratos consultaExtratos)
{
    public async Task<ExtratoDto> Handle(ObterExtrato consulta, CancellationToken cancellationToken)
    {
        var paginacao = Paginacao.Criar(consulta.Pagina, consulta.Tamanho);
        return await consultaExtratos.ObterAsync(consulta.ContaId, paginacao, cancellationToken)
            ?? throw new ExcecaoExtratoNaoEncontrado(consulta.ContaId);
    }
}
