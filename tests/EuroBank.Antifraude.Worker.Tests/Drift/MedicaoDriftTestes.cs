using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Worker.Observability;
using EuroBank.Antifraude.Worker.Services;
using EuroBank.Antifraude.Worker.Tests.Fixtures;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EuroBank.Antifraude.Worker.Tests.Drift;

/// <summary>
/// O ciclo de drift no worker: lê a versão ativa e as análises da janela no Postgres, grava a medição e atualiza as
/// métricas. Cada teste usa a própria janela (um dia distante) e a própria versão ativa; só esta classe mexe nelas.
/// </summary>
public sealed class MedicaoDriftTestes(AntifraudeWorkerFixture worker)
{
    /// <summary>O treino: metade com beneficiário novo; o score quase sempre baixo.</summary>
    private static readonly IReadOnlyList<DistribuicaoFeature> Referencia =
    [
        new("BeneficiarioNovo", [1], [0.5, 0.5]),
        new(DistribuicaoFeature.NomeScore, [0.5], [0.9, 0.1]),
    ];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MedicaoDriftServico Servico => worker.Servicos.GetRequiredService<MedicaoDriftServico>();

    /// <summary>Registra uma versão com a referência acima e a deixa como a única ativa.</summary>
    private async Task<int> AtivaAsync()
    {
        await using var contexto = worker.CriarContexto();
        await contexto.Database.ExecuteSqlRawAsync("UPDATE versoes_modelo SET status = 'Aposentada' WHERE status = 'Ativa'", Ct);
        var metricas = new MetricasModelo(0.9, 0.8, 0.9, 0.85, new MatrizConfusao(9, 2, 100, 1), []);
        var dados = new DadosTreino(42, 100, 2, 0, 0, Dia(1), Dia(2), Dia(3), 20, 0.9, 0.05, MetaAtingida: true);
        var versao = VersaoModelo.Registrar([1], metricas, 0.5, dados, Referencia, DateTimeOffset.UtcNow);
        versao.Ativar(DateTimeOffset.UtcNow);
        contexto.VersoesModelo.Add(versao);
        await contexto.SaveChangesAsync(Ct);
        return versao.Versao;
    }

    private static DateTimeOffset Dia(int dia) => new(2031, 3, dia, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Análises criadas uma hora antes de <paramref name="agora"/>, pontuadas pela versão (ou por outra).</summary>
    private async Task AnalisesAsync(DateTimeOffset agora, int quantidade, Func<int, float> beneficiarioNovo, int? versaoScore, float probabilidade = 0.1f)
    {
        await using var contexto = worker.CriarContexto();
        for (var indice = 0; indice < quantidade; indice++)
        {
            var operacao = new ContextoOperacao(
                Guid.CreateVersion7(), TipoOperacao.Externa, Guid.CreateVersion7(), Dinheiro.De(100m, Moeda.EUR), "DE89370400440532013000", agora);
            var features = new VetorFeatures(1, 0, 0, beneficiarioNovo(indice), 90, 15, 0.1f, 4.6f, 0, 0, 0);
            var modelo = versaoScore is { } versao ? new PontuacaoModelo(versao, probabilidade, [], 5) : null;
            contexto.Analises.Add(Analise.Registrar(operacao, new ResultadoMotor(DecisaoAnalise.Aprovada, 5, [], 1, features, modelo), agora.AddHours(-1)));
        }

        await contexto.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task ComoNoTreino_Estavel_EGravaAMedicao()
    {
        var versao = await AtivaAsync();
        var agora = Dia(10);
        await AnalisesAsync(agora, 120, indice => indice % 2, versao);
        // Análises fora da janela (dois dias antes) não entram.
        await AnalisesAsync(agora.AddDays(-2), 50, _ => 1, versao);

        var medicao = (await Servico.MedirAsync(agora, Ct)).ShouldNotBeNull();

        medicao.VersaoModelo.ShouldBe(versao);
        medicao.Situacao.ShouldBe("Medido");
        medicao.Amostra.ShouldBe(120);
        medicao.AmostraScore.ShouldBe(120);
        medicao.Psis.Single(psi => psi.Nome == "BeneficiarioNovo").Nivel.ShouldBe("Estavel");
        await using var contexto = worker.CriarContexto();
        var gravada = await contexto.MedicoesDrift.AsNoTracking()
            .Where(registro => registro.VersaoModelo == versao)
            .SingleAsync(Ct);
        gravada.MedidaEm.ShouldBe(agora);
        gravada.InicioJanela.ShouldBe(agora.AddHours(-24));
        gravada.Psis.Count.ShouldBe(medicao.Psis.Count);
        worker.Servicos.GetRequiredService<TelemetriaDrift>().Ultima.ShouldBe(medicao);
    }

    [Fact]
    public async Task TodosBeneficiariosNovos_Significativo_ScoreSoDaVersaoAtiva()
    {
        var versao = await AtivaAsync();
        var agora = Dia(14);
        await AnalisesAsync(agora, 110, _ => 1, versao, probabilidade: 0.9f);
        await AnalisesAsync(agora, 30, _ => 1, versaoScore: versao + 100);

        var medicao = (await Servico.MedirAsync(agora, Ct)).ShouldNotBeNull();

        medicao.Amostra.ShouldBe(140);
        medicao.AmostraScore.ShouldBe(110);
        medicao.Nivel.ShouldBe(nameof(NivelDrift.Significativo));
        medicao.Psis.Select(psi => psi.Nome).ShouldBe(["BeneficiarioNovo", DistribuicaoFeature.NomeScore], ignoreOrder: true);
        medicao.Psis.Select(psi => psi.Psi).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task PoucasAnalises_AmostraInsuficiente()
    {
        await AtivaAsync();
        var agora = Dia(18);
        await AnalisesAsync(agora, 20, _ => 1, versaoScore: null);

        var medicao = (await Servico.MedirAsync(agora, Ct)).ShouldNotBeNull();

        medicao.Situacao.ShouldBe("AmostraInsuficiente");
        medicao.Amostra.ShouldBe(20);
        medicao.Psis.ShouldBeEmpty();
        medicao.Nivel.ShouldBeNull();
    }

    [Fact]
    public async Task SemVersaoAtiva_NaoMede_EApagaAsAntigas()
    {
        await AtivaAsync();
        (await Servico.MedirAsync(Dia(20), Ct)).ShouldNotBeNull();
        await using (var contexto = worker.CriarContexto())
        {
            await contexto.Database.ExecuteSqlRawAsync("UPDATE versoes_modelo SET status = 'Aposentada' WHERE status = 'Ativa'", Ct);
        }

        // Oito dias depois: sem versão ativa, nada é medido, e a retenção de 7 dias apaga a medição acima.
        (await Servico.MedirAsync(Dia(28), Ct)).ShouldBeNull();

        await using var leitura = worker.CriarContexto();
        (await leitura.MedicoesDrift.AsNoTracking().AnyAsync(medicao => medicao.MedidaEm == Dia(20), Ct)).ShouldBeFalse();
        worker.Servicos.GetRequiredService<TelemetriaDrift>().Ultima.ShouldBeNull();
    }
}
