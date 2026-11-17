using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Uma medição de drift: as features das análises da janela (e o score das que a versão ativa pontuou) contra a
/// distribuição do treino dessa versão. Abaixo da amostra mínima, fica registrada como insuficiente, sem PSI; o score
/// só entra com a amostra mínima de análises pontuadas pela própria versão. O nível da medição é o pior entre os PSIs.
/// </summary>
public sealed class MedicaoDrift
{
    private static readonly Dictionary<string, int> IndiceFeature =
        VetorFeatures.Nomes.Select((nome, indice) => (Nome: nome, Indice: indice)).ToDictionary(par => par.Nome, par => par.Indice);

    private List<PsiFeature> _psis = [];

    private MedicaoDrift()
    {
        // Usado pelo EF Core.
    }

    public Guid Id { get; private set; }

    public int VersaoModelo { get; private set; }

    public DateTimeOffset MedidaEm { get; private set; }

    public DateTimeOffset InicioJanela { get; private set; }

    /// <summary>Análises com features na janela.</summary>
    public int Amostra { get; private set; }

    /// <summary>Delas, as pontuadas pela versão medida.</summary>
    public int AmostraScore { get; private set; }

    public SituacaoDrift Situacao { get; private set; }

    /// <summary>O pior nível entre as features e o score; nulo com amostra insuficiente.</summary>
    public NivelDrift? Nivel { get; private set; }

    public double? PsiMaximo { get; private set; }

    public IReadOnlyList<PsiFeature> Psis => _psis.AsReadOnly();

    public static MedicaoDrift Medir(
        int versaoModelo,
        IReadOnlyList<DistribuicaoFeature> referencia,
        IReadOnlyList<VetorFeatures> features,
        IReadOnlyList<double> scores,
        LimitesDrift limites,
        DateTimeOffset inicioJanela,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(referencia);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(limites);

        var medicao = new MedicaoDrift
        {
            Id = Guid.CreateVersion7(),
            VersaoModelo = versaoModelo,
            MedidaEm = agora,
            InicioJanela = inicioJanela,
            Amostra = features.Count,
            AmostraScore = scores.Count,
            Situacao = SituacaoDrift.AmostraInsuficiente,
        };
        if (features.Count < limites.AmostraMinima)
        {
            return medicao;
        }

        var vetores = features.Select(vetor => vetor.ParaArray()).ToList();
        var psis = new List<PsiFeature>();
        foreach (var distribuicao in referencia)
        {
            IReadOnlyCollection<double>? valores = IndiceFeature.TryGetValue(distribuicao.Nome, out var indice)
                ? vetores.Select(vetor => (double)vetor[indice]).ToList()
                : distribuicao.Nome == DistribuicaoFeature.NomeScore && scores.Count >= limites.AmostraMinima ? scores : null;
            if (valores is null)
            {
                continue;
            }

            var psi = CalculadoraDrift.Psi(distribuicao, valores);
            psis.Add(new PsiFeature(distribuicao.Nome, psi, CalculadoraDrift.Classificar(psi, limites)));
        }

        medicao._psis = psis;
        medicao.Situacao = SituacaoDrift.Medido;
        medicao.PsiMaximo = psis.Count == 0 ? 0 : psis.Max(psi => psi.Psi);
        medicao.Nivel = psis.Count == 0 ? NivelDrift.Estavel : psis.Max(psi => psi.Nivel);
        return medicao;
    }
}
