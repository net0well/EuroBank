using EuroBank.E2E.Tests.Fixtures;

namespace EuroBank.E2E.Tests;

/// <summary>Sagas que terminam Recusada antes de qualquer dinheiro sair da conta.</summary>
public sealed class SagaExternaRecusasTestes(LaboratorioFixture lab)
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    private readonly CenarioSaga _cenario = new(lab);

    [Fact]
    public async Task ContaSemSaldo_Recusada()
    {
        var conta = await _cenario.AbrirContaAsync(50m);

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
        var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("SaldoInsuficiente");
        (await _cenario.SaldoAsync(conta)).ShouldBe(50m);
    }

    [Fact]
    public async Task ContaBloqueada_Recusada()
    {
        var conta = await _cenario.AbrirContaAsync(500m);
        (await _cenario.ComandoContaAsync(conta, "bloqueio")).EnsureSuccessStatusCode();

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
        var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("ContaBloqueada");
        (await _cenario.SaldoAsync(conta)).ShouldBe(500m);
    }

    [Fact]
    public async Task ContaEncerrada_Recusada()
    {
        var conta = await _cenario.AbrirContaAsync(0m);
        (await _cenario.ComandoContaAsync(conta, "encerramento")).EnsureSuccessStatusCode();

        var pedido = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);
        var recusada = await _cenario.EsperarEstadoAsync(pedido.Id, "Recusada", Limite);

        recusada.Motivo.ShouldBe("ContaEncerrada");
    }

    [Fact]
    public async Task LimiteDiario_4950JaTransferidos_RecusaMais100_SemContarATarifa()
    {
        var conta = await _cenario.AbrirContaAsync(6_000m);
        var primeira = await _cenario.SolicitarComSucessoAsync(conta, "00", 4_950m);
        await _cenario.EsperarEstadoAsync(primeira.Id, "Concluida", Limite);

        var segunda = await _cenario.SolicitarComSucessoAsync(conta, "00", 100m);

        segunda.Estado.ShouldBe("Recusada");
        segunda.Motivo.ShouldBe("LimiteDiarioExcedido");
        (await _cenario.SaldoAsync(conta)).ShouldBe(6_000m - 4_952.50m);
    }
}
