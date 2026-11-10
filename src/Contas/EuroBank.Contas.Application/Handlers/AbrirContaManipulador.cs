using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;

namespace EuroBank.Contas.Application.Handlers;

public sealed class AbrirContaManipulador(
    IRepositorioContas repositorio,
    IGeradorIban geradorIban,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<ContaDto> Handle(AbrirConta comando, CancellationToken cancellationToken)
    {
        // Valida titular e moeda antes de consumir um número da sequence.
        var titular = Titular.Criar(comando.Titular);
        var moeda = string.IsNullOrWhiteSpace(comando.Moeda) ? Moeda.EUR : Moedas.Ler(comando.Moeda);
        var iban = await geradorIban.GerarAsync(cancellationToken);

        var conta = Conta.Abrir(iban, titular, relogio.GetUtcNow(), moeda);
        repositorio.Adicionar(conta);

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return conta.ParaDto();
    }
}
