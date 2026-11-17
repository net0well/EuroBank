using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Enums;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class SacarManipulador(
    IRepositorioContas repositorio,
    ConferenciaAntifraude antifraude,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<ContaDto> Handle(Sacar comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);
        var valor = Dinheiro.De(comando.Valor, conta.Moeda);
        var agora = relogio.GetUtcNow();
        await antifraude.GarantirAprovadaAsync(
            TipoOperacaoRisco.Saque, conta.Id.Valor, valor, null, comando.DispositivoId, comando.Localizacao, agora, cancellationToken);

        conta.Sacar(valor, agora);

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return conta.ParaDto();
    }
}
