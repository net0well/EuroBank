using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Queries;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Application.Handlers;

public sealed class ListarTransferenciasManipulador(IConsultaTransferencias consulta)
{
    public Task<ResultadoPaginado<TransferenciaResumoDto>> Handle(
        ListarTransferencias pedido,
        CancellationToken cancellationToken)
    {
        var filtro = new FiltroTransferencias(
            pedido.ContaOrigemId,
            Ler<TipoTransferencia>("tipo", pedido.Tipo),
            Ler<EstadoTransferencia>("status", pedido.Status));

        return consulta.ListarAsync(filtro, Paginacao.Criar(pedido.Pagina, pedido.Tamanho), cancellationToken);
    }

    private static TEnum? Ler<TEnum>(string nome, string? valor)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return Enum.TryParse<TEnum>(valor, ignoreCase: true, out var lido) && Enum.IsDefined(lido) && !int.TryParse(valor, out _)
            ? lido
            : throw new ExcecaoFiltroInvalido(nome, valor);
    }
}
