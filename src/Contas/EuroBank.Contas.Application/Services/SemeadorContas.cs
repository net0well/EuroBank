using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Services;

/// <summary>
/// Cria contas de exemplo em Development quando o banco está vazio. Passa pelo domínio, então os eventos vão
/// para a outbox e chegam ao Extrato como qualquer outra operação.
/// </summary>
public sealed class SemeadorContas(
    IRepositorioContas repositorio,
    IGeradorIban geradorIban,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    private static readonly (string Titular, decimal DepositoInicial)[] ContasExemplo =
    [
        ("Ana", 1000m),
        ("Bruno", 500m),
        ("Carla", 250m),
    ];

    public async Task SemearAsync(CancellationToken cancellationToken)
    {
        if (await repositorio.ExisteAlgumaAsync(cancellationToken))
        {
            return;
        }

        foreach (var (titular, depositoInicial) in ContasExemplo)
        {
            var agora = relogio.GetUtcNow();
            var conta = Conta.Abrir(await geradorIban.GerarAsync(cancellationToken), Titular.Criar(titular), agora);
            conta.Depositar(Dinheiro.De(depositoInicial), agora);
            repositorio.Adicionar(conta);
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
