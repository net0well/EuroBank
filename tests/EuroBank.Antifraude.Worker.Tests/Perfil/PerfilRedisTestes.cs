using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Worker.Tests.Fixtures;

namespace EuroBank.Antifraude.Worker.Tests.Perfil;

/// <summary>
/// O perfil de comportamento no Redis, montado pelos eventos de Contas: idade, saldo, saídas dos últimos 31 dias e
/// beneficiários conhecidos. Cada evento só vale se a versão da conta for nova (repetido ou fora de ordem: ignorado).
/// </summary>
public sealed class PerfilRedisTestes(AntifraudeWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    private const string IbanExterno = "DE89370400440532013000";

    private const string IbanInterno = "PT50EURO0000000000002";

    private static EventoSaidaConta Saida(Guid conta, int versao, decimal valor, decimal saldoApos, DateTimeOffset quando, string iban = IbanExterno) =>
        new(conta, Guid.CreateVersion7(), iban, valor, saldoApos, versao, quando);

    private async Task<Guid> ContaComDepositoAsync(decimal saldo = 1_000m, DateTimeOffset? abertaEm = null)
    {
        var conta = Guid.CreateVersion7();
        await worker.EntregarAsync(new EventoContaAberta(conta, "PT50EURO0000000000001", "Ana", 1, abertaEm ?? Agora.AddDays(-60), "EUR"));
        await worker.EntregarAsync(new EventoMovimentoConta(conta, saldo, 2, Agora.AddDays(-59)));
        return conta;
    }

    [Fact]
    public async Task Entradas_FicamNoHistorico_ComOSaldo()
    {
        var conta = await ContaComDepositoAsync(100m);

        await worker.EntregarAsync(new EventoEntradaConta(conta, 2_500m, 2_600m, 3, Agora.AddHours(-2)));
        await worker.EntregarAsync(new EventoEntradaConta(conta, 400m, 3_000m, 4, Agora.AddHours(-1)));
        await worker.EntregarAsync(new EventoEntradaConta(conta, 400m, 3_000m, 4, Agora.AddHours(-1)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saldo.ShouldBe(3_000m);
        perfil.Entradas.Select(entrada => entrada.Valor).ShouldBe([2_500m, 400m], ignoreOrder: true);
        perfil.Entradas.ShouldContain(entrada => entrada.Momento == Agora.AddHours(-2));
    }

    [Fact]
    public async Task ContaAbertaEDeposito_DaoIdadeESaldo()
    {
        var conta = await ContaComDepositoAsync(500m, Agora.AddDays(-60));

        var perfil = await worker.PerfilAsync(conta, Agora);

        perfil.ContaId.ShouldBe(conta);
        perfil.AbertaEm.ShouldBe(Agora.AddDays(-60));
        perfil.Saldo.ShouldBe(500m);
        perfil.Saidas.ShouldBeEmpty();
        perfil.BeneficiariosConhecidos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Saidas_GuardamValorIbanEMomento_EOBeneficiarioFicaConhecido()
    {
        var conta = await ContaComDepositoAsync();

        await worker.EntregarAsync(Saida(conta, 3, 100m, 900m, Agora.AddHours(-2)));
        await worker.EntregarAsync(Saida(conta, 4, 50.25m, 849.75m, Agora.AddMinutes(-5), "pt50 euro 0000 0000 0000 2"));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saldo.ShouldBe(849.75m);
        perfil.Saidas.OrderBy(saida => saida.Momento).ShouldBe(
        [
            new SaidaConta(Agora.AddHours(-2), 100m, IbanExterno),
            new SaidaConta(Agora.AddMinutes(-5), 50.25m, IbanInterno),
        ]);
        perfil.BeneficiariosConhecidos.ShouldBe([IbanExterno, IbanInterno], ignoreOrder: true);
    }

    [Fact]
    public async Task EventoRepetidoOuAtrasado_NaoMudaNada()
    {
        var conta = await ContaComDepositoAsync();
        var saida = Saida(conta, 3, 100m, 900m, Agora.AddHours(-1));

        await worker.EntregarAsync(saida);
        await worker.EntregarAsync(saida);
        await worker.EntregarAsync(new EventoMovimentoConta(conta, 12_345m, 2, Agora.AddDays(-59)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saidas.ShouldHaveSingleItem();
        perfil.Saldo.ShouldBe(900m);
    }

    [Fact]
    public async Task SaidasDeMaisDe31Dias_SaemDoPerfil()
    {
        var conta = await ContaComDepositoAsync(abertaEm: Agora.AddDays(-90));

        await worker.EntregarAsync(Saida(conta, 3, 70m, 930m, Agora.AddDays(-40)));
        await worker.EntregarAsync(Saida(conta, 4, 30m, 900m, Agora.AddDays(-1)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saidas.ShouldHaveSingleItem().Valor.ShouldBe(30m);
        perfil.BeneficiariosConhecidos.ShouldContain(IbanExterno);
    }

    [Fact]
    public async Task Estorno_DevolveOSaldo_MasASaidaContinuaNoPerfil()
    {
        var conta = await ContaComDepositoAsync();

        await worker.EntregarAsync(Saida(conta, 3, 100m, 900m, Agora.AddMinutes(-10)));
        await worker.EntregarAsync(new EventoMovimentoConta(conta, 1_000m, 4, Agora.AddMinutes(-9)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saldo.ShouldBe(1_000m);
        perfil.Saidas.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Bloqueio_NaoMexeNoSaldoNemNasSaidas()
    {
        var conta = await ContaComDepositoAsync(700m);

        await worker.EntregarAsync(new EventoStatusConta(conta, 3, Agora.AddDays(-1)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Saldo.ShouldBe(700m);
        perfil.Saidas.ShouldBeEmpty();
    }

    [Fact]
    public async Task ContaQueOWorkerNuncaViu_TemPerfilVazio()
    {
        var conta = Guid.CreateVersion7();

        var perfil = await worker.PerfilAsync(conta, Agora);

        perfil.ContaId.ShouldBe(conta);
        perfil.AbertaEm.ShouldBeNull();
        perfil.Saldo.ShouldBeNull();
        perfil.Saidas.ShouldBeEmpty();
        perfil.BeneficiariosConhecidos.ShouldBeEmpty();
    }

    /// <summary>
    /// Fica nesta classe (os testes de uma classe rodam em sequência) porque apaga o Redis inteiro: outra classe
    /// rodando ao mesmo tempo perderia os dados dela.
    /// </summary>
    [Fact]
    public async Task RedisApagado_ReaplicarOsEventosReconstroiOMesmoPerfil()
    {
        var conta = Guid.CreateVersion7();
        object[] eventos =
        [
            new EventoContaAberta(conta, "PT50EURO0000000000009", "Bia", 1, Agora.AddDays(-20), "USD"),
            new EventoMovimentoConta(conta, 2_000m, 2, Agora.AddDays(-19)),
            Saida(conta, 3, 150m, 1_850m, Agora.AddDays(-3)),
            Saida(conta, 4, 80m, 1_770m, Agora.AddHours(-6), IbanInterno),
            new EventoStatusConta(conta, 5, Agora.AddHours(-1)),
        ];
        foreach (var evento in eventos)
        {
            await worker.EntregarAsync(evento);
        }

        var antes = await worker.PerfilAsync(conta, Agora);

        await worker.ApagarPerfilAsync(conta);
        (await worker.PerfilAsync(conta, Agora)).Saldo.ShouldBeNull();
        foreach (var evento in eventos)
        {
            await worker.EntregarAsync(evento);
        }

        var depois = await worker.PerfilAsync(conta, Agora);
        depois.AbertaEm.ShouldBe(antes.AbertaEm);
        depois.Saldo.ShouldBe(antes.Saldo);
        depois.Saidas.OrderBy(saida => saida.Momento).ShouldBe(antes.Saidas.OrderBy(saida => saida.Momento));
        depois.BeneficiariosConhecidos.ShouldBe(antes.BeneficiariosConhecidos, ignoreOrder: true);
    }
}
