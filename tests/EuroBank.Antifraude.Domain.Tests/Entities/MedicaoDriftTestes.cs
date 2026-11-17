using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

/// <summary>A medição: amostra mínima, o score só com análises da própria versão e o nível pelo pior PSI.</summary>
public sealed class MedicaoDriftTestes
{
    private static readonly LimitesDrift Limites = new(amostraMinima: 10, moderado: 0.1, significativo: 0.25);

    /// <summary>O treino: metade das contas com beneficiário novo; valores sempre na mesma faixa; score quase sempre baixo.</summary>
    private static readonly IReadOnlyList<DistribuicaoFeature> Referencia =
    [
        new("BeneficiarioNovo", [0], [0.5, 0.5]),
        new("LogValor", [3, 6], [0.1, 0.8, 0.1]),
        new(DistribuicaoFeature.NomeScore, [0.5], [0.9, 0.1]),
    ];

    private static VetorFeatures Vetor(float beneficiarioNovo, float logValor) => new(1, 0, 0, beneficiarioNovo, 90, 15, 0.1f, logValor, 0, 0, 0);

    private static List<VetorFeatures> Amostra(int quantidade, Func<int, VetorFeatures> vetor) => Enumerable.Range(0, quantidade).Select(vetor).ToList();

    /// <summary>LogValor como no treino: 10% abaixo de 3, 80% entre 3 e 6, 10% acima.</summary>
    private static float LogValorComoNoTreino(int indice) => indice % 10 == 0 ? 2 : indice % 10 == 9 ? 7 : 4.5f;

    private static MedicaoDrift Medir(IReadOnlyList<VetorFeatures> features, IReadOnlyList<double>? scores = null) =>
        MedicaoDrift.Medir(5, Referencia, features, scores ?? [], Limites, Agora.AddHours(-24), Agora);

    [Fact]
    public void ComoNoTreino_Estavel()
    {
        var medicao = Medir(Amostra(20, indice => Vetor(indice % 2, LogValorComoNoTreino(indice))), [.. Enumerable.Repeat(0.1, 18), 0.9, 0.9]);

        medicao.Situacao.ShouldBe(SituacaoDrift.Medido);
        medicao.VersaoModelo.ShouldBe(5);
        medicao.Amostra.ShouldBe(20);
        medicao.AmostraScore.ShouldBe(20);
        medicao.InicioJanela.ShouldBe(Agora.AddHours(-24));
        medicao.Psis.Select(psi => psi.Nome).ShouldBe(["BeneficiarioNovo", "LogValor", DistribuicaoFeature.NomeScore]);
        medicao.Psis.ShouldAllBe(psi => Math.Abs(psi.Psi) < 1e-9);
        medicao.Nivel.ShouldBe(NivelDrift.Estavel);
        medicao.PsiMaximo.ShouldNotBeNull().ShouldBe(0, tolerance: 1e-9);
    }

    [Fact]
    public void TodosBeneficiariosNovos_Significativo_PeloPiorPsi()
    {
        var medicao = Medir(Amostra(20, indice => Vetor(1, LogValorComoNoTreino(indice))));

        medicao.Psis.Single(psi => psi.Nome == "BeneficiarioNovo").Nivel.ShouldBe(NivelDrift.Significativo);
        medicao.Psis.Single(psi => psi.Nome == "LogValor").Nivel.ShouldBe(NivelDrift.Estavel);
        medicao.Nivel.ShouldBe(NivelDrift.Significativo);
        medicao.PsiMaximo.ShouldBe(medicao.Psis.Max(psi => psi.Psi));
    }

    [Fact]
    public void AbaixoDaAmostraMinima_Insuficiente_SemPsi()
    {
        var medicao = Medir(Amostra(9, _ => Vetor(1, 4.5f)));

        medicao.Situacao.ShouldBe(SituacaoDrift.AmostraInsuficiente);
        medicao.Amostra.ShouldBe(9);
        medicao.Psis.ShouldBeEmpty();
        medicao.Nivel.ShouldBeNull();
        medicao.PsiMaximo.ShouldBeNull();
    }

    [Fact]
    public void PoucasAnalisesPontuadas_ScoreFicaDeFora()
    {
        var medicao = Medir(Amostra(20, indice => Vetor(indice % 2, LogValorComoNoTreino(indice))), [0.1, 0.2]);

        medicao.Situacao.ShouldBe(SituacaoDrift.Medido);
        medicao.AmostraScore.ShouldBe(2);
        medicao.Psis.ShouldNotContain(psi => psi.Nome == DistribuicaoFeature.NomeScore);
    }
}
