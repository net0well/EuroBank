using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Contas.Worker.Messaging;
using EuroBank.Contas.Worker.Tests.Fixtures;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Contas.Worker.Tests;

public sealed class ComandosSagaWorkerTestes(ContasWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("DebitarTransferenciaExterna", typeof(DebitarTransferenciaExterna))]
    [InlineData("EstornarTransferencia", typeof(EstornarTransferencia))]
    [InlineData("LiberarTravaTransferencia", typeof(LiberarTravaTransferencia))]
    public void TabelaDeComandos_TemOsTresComandosDaSaga(string tipo, Type esperado)
    {
        TiposComandosSaga.Mapa[tipo].ShouldBe(esperado);
    }

    [Fact]
    public async Task ComandosDaSaga_DebitamEstornamERespondemPelaOutbox()
    {
        var contaId = await CriarContaAsync(500m);
        var transferenciaId = Guid.CreateVersion7();

        await worker.EntregarAsync(new DebitarTransferenciaExterna(
            transferenciaId, contaId.Valor, 100m, 2.50m, "DE89370400440532013000", "Bruno", null));
        await worker.EntregarAsync(new EstornarTransferencia(transferenciaId, contaId.Valor));

        await using var leitura = worker.CriarContexto();
        (await leitura.Contas.SingleAsync(c => c.Id == contaId, Ct)).Saldo.ShouldBe(Dinheiro.De(500m));
        var respostas = await leitura.Outbox
            .Where(m => m.IdAgregado == transferenciaId.ToString())
            .OrderBy(m => m.Sequencia)
            .Select(m => m.TipoEvento)
            .ToListAsync(Ct);
        respostas.ShouldBe(["DebitoRealizado", "EstornoRealizado"]);
    }

    private async Task<ContaId> CriarContaAsync(decimal saldo)
    {
        await using var contexto = worker.CriarContexto();
        var conta = Conta.Abrir(Iban.Gerar(Random.Shared.NextInt64(1, 9_000_000_000_000L)), Titular.Criar("Worker"), Agora);
        conta.Depositar(Dinheiro.De(saldo), Agora);
        conta.LimparEventos();
        contexto.Contas.Add(conta);
        await contexto.SaveChangesAsync(Ct);
        return conta.Id;
    }
}
