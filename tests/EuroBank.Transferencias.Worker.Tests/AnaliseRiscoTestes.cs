using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Worker.Messaging;
using EuroBank.Transferencias.Worker.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Transferencias.Worker.Tests;

/// <summary>
/// O passo de risco no worker: a varredura chama o antifraude e aplica a decisão; a decisão do analista chega pelo
/// evento AnaliseDecidida (ou pela próxima consulta, se o evento se perder).
/// </summary>
[Collection(nameof(ColecaoWorker))]
public sealed class AnaliseRiscoTestes(TransferenciasWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private PoliticaSaga Politica => worker.Servicos.GetRequiredService<PoliticaSaga>();

    private EventoAnaliseDecidida Decidida(TransferenciaId id, string decisao, string origem = "Analista") =>
        new(worker.Antifraude.AnaliseDe(id), id.Valor, decisao, origem, worker.Relogio.GetUtcNow());

    [Fact]
    public async Task AntifraudeAprova_VarreduraPedeODebito()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
        (await worker.ComandosAsync(id, conta)).ShouldBeEmpty();
        worker.Antifraude.Responder(id, DecisaoRisco.Aprovada);

        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);

        var aprovada = await worker.ObterAsync(id);
        aprovada.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        aprovada.AnaliseRiscoId.ShouldBe(worker.Antifraude.AnaliseDe(id));
        worker.Antifraude.ValoresEnviados(id).ShouldBe([100m]);
        (await worker.ComandosAsync(id, conta)).ShouldBe(["DebitarTransferenciaExterna"]);
    }

    [Fact]
    public async Task AntifraudeRecusa_TerminaRecusadaSemDebito()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        worker.Antifraude.Responder(id, DecisaoRisco.Recusada);

        await AvancarAsync(id);

        var recusada = await worker.ObterAsync(id);
        recusada.Estado.ShouldBe(EstadoTransferencia.Recusada);
        recusada.Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloAntifraude);
        (await worker.ComandosAsync(id, conta)).ShouldBe(["TransferenciaRecusada"]);
    }

    [Fact]
    public async Task EmRevisao_AnalistaAprova_PeloEvento()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        var caso = Guid.CreateVersion7();
        worker.Antifraude.Responder(id, DecisaoRisco.EmRevisao, caso);

        await AvancarAsync(id);

        var emRevisao = await worker.ObterAsync(id);
        emRevisao.Estado.ShouldBe(EstadoTransferencia.EmAnaliseManual);
        emRevisao.CasoRevisaoId.ShouldBe(caso);
        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Ignorado);

        await worker.EntregarAsync(Decidida(id, "Aprovada"));

        var aprovada = await worker.ObterAsync(id);
        aprovada.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        aprovada.CasoRevisaoId.ShouldBe(caso);
        (await worker.ComandosAsync(id, conta)).ShouldBe(["DebitarTransferenciaExterna"]);
    }

    [Theory]
    [InlineData("Recusada", MotivoTransferencia.RecusadaPeloAntifraude)]
    [InlineData("Expirada", MotivoTransferencia.RevisaoExpirada)]
    public async Task EmRevisao_AnalistaRecusaOuCasoExpira_PeloEvento(string decisao, MotivoTransferencia motivo)
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        worker.Antifraude.Responder(id, DecisaoRisco.EmRevisao, Guid.CreateVersion7());
        await AvancarAsync(id);

        await worker.EntregarAsync(Decidida(id, decisao, decisao == "Expirada" ? "Expiracao" : "Analista"));

        var recusada = await worker.ObterAsync(id);
        recusada.Estado.ShouldBe(EstadoTransferencia.Recusada);
        recusada.Motivo.ShouldBe(motivo);
    }

    [Fact]
    public async Task EventoPerdido_AProximaConsultaTrazADecisao()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        var caso = Guid.CreateVersion7();
        worker.Antifraude.Responder(id, DecisaoRisco.EmRevisao, caso);
        await AvancarAsync(id);

        worker.Antifraude.Responder(id, DecisaoRisco.Aprovada, caso);
        worker.Relogio.Advance(Politica.IntervaloConsultaRevisao);

        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);
        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
    }

    [Fact]
    public async Task AntifraudeForaDoAr_TentaDeNovo_EDepoisDoPrazoRecusa()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        var conta = (await worker.ObterAsync(id)).ContaOrigemId;
        worker.Antifraude.ForaDoAr(id);

        (await AvancarAsync(id)).ShouldBe(ResultadoPasso.Aplicado);
        var tentando = await worker.ObterAsync(id);
        tentando.Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
        tentando.TentativasNoPasso.ShouldBe(1);
        tentando.PrazoDoPasso.ShouldBe(worker.Relogio.GetUtcNow() + Politica.IntervaloAnaliseRisco);

        worker.Relogio.Advance(Politica.PrazoAnaliseRisco);
        await AvancarAsync(id);

        var recusada = await worker.ObterAsync(id);
        recusada.Estado.ShouldBe(EstadoTransferencia.Recusada);
        recusada.Motivo.ShouldBe(MotivoTransferencia.AntifraudeIndisponivel);
        (await worker.ComandosAsync(id, conta)).ShouldBe(["TransferenciaRecusada"]);
    }

    [Fact]
    public async Task DecisaoDasRegrasPeloEvento_DepoisDaVarredura_NaoMudaNemEntraNoHistorico()
    {
        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        worker.Antifraude.Responder(id, DecisaoRisco.Aprovada);
        await AvancarAsync(id);
        var versao = (await worker.ObterAsync(id)).Versao;

        await worker.EntregarAsync(Decidida(id, "Aprovada", origem: "Motor"));

        var transferencia = await worker.ObterAsync(id);
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.Versao.ShouldBe(versao);
    }

    [Fact]
    public async Task DecisaoDeOperacaoDesconhecida_OuComDecisaoInvalida_EIgnorada()
    {
        await worker.EntregarAsync(new EventoAnaliseDecidida(Guid.CreateVersion7(), Guid.CreateVersion7(), "Aprovada", "Motor", worker.Relogio.GetUtcNow()));

        var id = await worker.CriarAsync(comAnaliseDeRisco: true);
        await worker.EntregarAsync(Decidida(id, "Talvez"));

        (await worker.ObterAsync(id)).Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
    }

    [Fact]
    public void TabelaDoAntifraude_SoAnaliseDecidida()
    {
        TiposRespostasSaga.Antifraude.ShouldHaveSingleItem().ShouldBe(
            new KeyValuePair<string, Type>("AnaliseDecidida", typeof(EventoAnaliseDecidida)));
    }

    private async Task<ResultadoPasso> AvancarAsync(TransferenciaId id)
    {
        await using var escopo = worker.Servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<VarreduraTransferencias>().AvancarAsync(id, Ct);
    }
}
