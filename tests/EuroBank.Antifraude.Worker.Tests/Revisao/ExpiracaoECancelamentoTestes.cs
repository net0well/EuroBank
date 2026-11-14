using System.Text.Json;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Infrastructure.Messaging;
using EuroBank.Antifraude.Worker.Services;
using EuroBank.Antifraude.Worker.Tests.Fixtures;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Worker.Tests.Revisao;

/// <summary>Casos que passam do prazo expiram; cancelamento pelo cliente tira o caso da fila.</summary>
public sealed class ExpiracaoECancelamentoTestes(AntifraudeWorkerFixture worker)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Análise em revisão com o caso aberto em <paramref name="abertoEm"/> e prazo de 30 minutos.</summary>
    private async Task<(Guid OperacaoId, CasoId CasoId, AnaliseId AnaliseId)> EmRevisaoAsync(DateTimeOffset abertoEm)
    {
        var operacao = new ContextoOperacao(
            Guid.CreateVersion7(), TipoOperacao.Externa, Guid.CreateVersion7(), Dinheiro.De(4_600m, Moeda.EUR), "DE89370400440532013000", abertoEm);
        var analise = Analise.Registrar(operacao, new ResultadoMotor(DecisaoAnalise.EmRevisao, 50, [], 1), abertoEm);
        var caso = CasoRevisao.Abrir(analise.Id, operacao.OperacaoId, operacao.ContaId, 50, TimeSpan.FromMinutes(30), abertoEm);
        await worker.EmEscopoAsync(async servicos =>
        {
            servicos.GetRequiredService<IRepositorioAnalises>().Adicionar(analise);
            servicos.GetRequiredService<IRepositorioCasos>().Adicionar(caso);
            await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAsync(Ct);
        });
        return (operacao.OperacaoId, caso.Id, analise.Id);
    }

    private async Task<(EstadoCaso Caso, DecisaoAnalise Analise)> EstadoAsync(CasoId casoId, AnaliseId analiseId)
    {
        await using var contexto = worker.CriarContexto();
        var caso = await contexto.Casos.AsNoTracking().SingleAsync(c => c.Id == casoId, Ct);
        var analise = await contexto.Analises.AsNoTracking().SingleAsync(a => a.Id == analiseId, Ct);
        return (caso.Estado, analise.Decisao);
    }

    [Fact]
    public async Task CasoVencido_Expira_EADecisaoVaiParaAOutbox()
    {
        var (operacaoId, casoId, analiseId) = await EmRevisaoAsync(DateTimeOffset.UtcNow.AddHours(-1));

        await worker.Servicos.GetRequiredService<ExpiracaoCasosServico>().ExpirarVencidosAsync(Ct);

        (await EstadoAsync(casoId, analiseId)).ShouldBe((EstadoCaso.Expirado, DecisaoAnalise.Expirada));
        await using var contexto = worker.CriarContexto();
        var mensagem = await contexto.Outbox.AsNoTracking()
            .SingleAsync(m => m.IdAgregado == operacaoId.ToString() && m.TipoEvento == "AnaliseDecidida", Ct);
        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("decisao").GetString().ShouldBe("Expirada");
        payload.RootElement.GetProperty("origem").GetString().ShouldBe("Expiracao");
    }

    [Fact]
    public async Task CasoNoPrazo_NaoExpira()
    {
        var (_, casoId, analiseId) = await EmRevisaoAsync(DateTimeOffset.UtcNow);

        await worker.Servicos.GetRequiredService<ExpiracaoCasosServico>().ExpirarVencidosAsync(Ct);

        (await EstadoAsync(casoId, analiseId)).ShouldBe((EstadoCaso.Aberto, DecisaoAnalise.EmRevisao));
    }

    [Fact]
    public async Task CanceladaPeloCliente_FechaOCaso()
    {
        var (operacaoId, casoId, analiseId) = await EmRevisaoAsync(DateTimeOffset.UtcNow);

        await worker.EntregarAsync(new EventoTransferenciaCancelada(operacaoId, Guid.CreateVersion7(), "CanceladaPeloCliente", DateTimeOffset.UtcNow));
        await worker.EntregarAsync(new EventoTransferenciaCancelada(operacaoId, Guid.CreateVersion7(), "CanceladaPeloCliente", DateTimeOffset.UtcNow));

        (await EstadoAsync(casoId, analiseId)).ShouldBe((EstadoCaso.CanceladoPeloCliente, DecisaoAnalise.Cancelada));
    }

    [Fact]
    public async Task CanceladaPorOutroMotivo_NaoMexeNoCaso()
    {
        var (operacaoId, casoId, analiseId) = await EmRevisaoAsync(DateTimeOffset.UtcNow);

        await worker.EntregarAsync(new EventoTransferenciaCancelada(operacaoId, Guid.CreateVersion7(), "RecusadaPeloBanco", DateTimeOffset.UtcNow));
        await worker.EntregarAsync(new EventoTransferenciaSemEfeito(operacaoId));

        (await EstadoAsync(casoId, analiseId)).ShouldBe((EstadoCaso.Aberto, DecisaoAnalise.EmRevisao));
    }

    [Theory]
    [InlineData("TransferenciaCancelada", typeof(EventoTransferenciaCancelada))]
    [InlineData("TransferenciaConcluida", typeof(EventoTransferenciaSemEfeito))]
    [InlineData("TransferenciaRecusada", typeof(EventoTransferenciaSemEfeito))]
    [InlineData("TransferenciaDivergente", typeof(EventoTransferenciaSemEfeito))]
    [InlineData("TransferenciaDevolvida", typeof(EventoTransferenciaSemEfeito))]
    public void EventosDeTransferencias_TodosConhecidos(string tipo, Type esperado)
    {
        TiposEventosTransferencias.Mapa[tipo].ShouldBe(esperado);
    }
}
