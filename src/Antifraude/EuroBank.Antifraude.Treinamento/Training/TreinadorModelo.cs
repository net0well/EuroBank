using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Infrastructure.MachineLearning;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace EuroBank.Antifraude.Treinamento.Training;

/// <summary>
/// Treina o FastTree (árvores com boosting) e o avalia como a produção o usaria. Teste temporal: os dias mais recentes
/// dos dados sintéticos ficam de fora do treino; os rótulos reais entram todos no treino (são poucos e não têm a
/// ordem do mundo sintético). A fraude é rara, então cada fraude pesa no treino o que pesam as normais juntas
/// (negativos / positivos). O arquivo salvo é a cadeia modelo + contribuições das features: a produção só o carrega
/// e recebe, junto da probabilidade, quanto cada feature empurrou o score.
/// </summary>
public sealed class TreinadorModelo(OpcoesTreino opcoes)
{
    public ResultadoTreino Treinar(int sementeDados, IReadOnlyList<LinhaTreino> sinteticas, IReadOnlyList<LinhaTreino> reais)
    {
        ArgumentNullException.ThrowIfNull(sinteticas);
        ArgumentNullException.ThrowIfNull(reais);

        var ordenadas = sinteticas.OrderBy(linha => linha.Momento).ToList();
        if (ordenadas.Count < 2)
        {
            throw new InvalidOperationException("Dados sintéticos insuficientes para separar treino e teste.");
        }

        var corte = ordenadas[(int)(ordenadas.Count * (1 - opcoes.FracaoTeste))].Momento;
        var treino = ordenadas.Where(linha => linha.Momento < corte).Concat(reais).ToList();
        var teste = ordenadas.Where(linha => linha.Momento >= corte).ToList();
        ExigirAsDuasClasses(treino, "treino");
        ExigirAsDuasClasses(teste, "teste");

        var positivos = treino.Count(linha => linha.Fraude);
        var pesoPositivo = (float)(treino.Count - positivos) / positivos;

        var ml = new MLContext(opcoes.Semente);
        var dadosTreino = ml.Data.LoadFromEnumerable(
            treino.Select(linha => EntradaModelo.De(linha.Features, linha.Fraude, linha.Fraude ? pesoPositivo : 1)));
        var arquivo = Ajustar(ml, dadosTreino);

        // Avalia o arquivo recarregado, não o objeto em memória: é o que a produção vai pontuar.
        var modelo = ml.Model.Load(new MemoryStream(arquivo), out _);
        var previsoesTeste = modelo.Transform(ml.Data.LoadFromEnumerable(teste.Select(linha => EntradaModelo.De(linha.Features, linha.Fraude))));
        var aucPr = ml.BinaryClassification.Evaluate(previsoesTeste, labelColumnName: nameof(EntradaModelo.Label)).AreaUnderPrecisionRecallCurve;
        var probabilidadesTeste = Probabilidades(ml, previsoesTeste);
        var rotuladas = teste.Zip(probabilidadesTeste, (linha, probabilidade) => (linha.Fraude, probabilidade)).ToList();
        var escolha = AvaliadorLimiares.Recomendar(rotuladas, opcoes.MetaRecall, opcoes.MetaTaxaSinalizada);

        var metricas = new MetricasModelo(aucPr, escolha.Precisao, escolha.Recall, escolha.F1, escolha.Matriz, AvaliadorLimiares.Curva(rotuladas));
        var dados = new DadosTreino(
            sementeDados,
            sinteticas.Count,
            sinteticas.Count(linha => linha.Fraude),
            reais.Count,
            reais.Count(linha => linha.Fraude),
            InicioTreino: ordenadas[0].Momento,
            CorteTeste: corte,
            FimTeste: ordenadas[^1].Momento,
            LinhasTeste: teste.Count,
            opcoes.MetaRecall,
            opcoes.MetaTaxaSinalizada,
            escolha.MetaAtingida);

        return new ResultadoTreino(arquivo, metricas, escolha.Limiar, dados, Distribuicao(treino, Probabilidades(ml, modelo.Transform(dadosTreino))));
    }

    private byte[] Ajustar(MLContext ml, IDataView dadosTreino)
    {
        var treinador = ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
        {
            LabelColumnName = nameof(EntradaModelo.Label),
            FeatureColumnName = nameof(EntradaModelo.Features),
            ExampleWeightColumnName = nameof(EntradaModelo.Peso),
            NumberOfTrees = opcoes.Arvores,
            NumberOfLeaves = opcoes.Folhas,
            MinimumExampleCountPerLeaf = opcoes.MinimoPorFolha,
            LearningRate = opcoes.TaxaAprendizado,
            Seed = opcoes.Semente,
        });
        var modelo = treinador.Fit(dadosTreino);

        // Todas as features nas contribuições (o padrão guarda só as 10 maiores de cada sinal), sem normalizar: o
        // valor é a parcela do score bruto, que a explicação da decisão mostra.
        var contribuicoes = ml.Transforms.CalculateFeatureContribution(
                modelo,
                numberOfPositiveContributions: EntradaModelo.TamanhoVetor,
                numberOfNegativeContributions: EntradaModelo.TamanhoVetor,
                normalize: false)
            .Fit(dadosTreino);

        using var arquivo = new MemoryStream();
        ml.Model.Save(modelo.Append(contribuicoes), dadosTreino.Schema, arquivo);
        return arquivo.ToArray();
    }

    private static List<float> Probabilidades(MLContext ml, IDataView previsoes) =>
        ml.Data.CreateEnumerable<SaidaModelo>(previsoes, reuseRowObject: false).Select(saida => saida.Probability).ToList();

    private static List<DistribuicaoFeature> Distribuicao(IReadOnlyList<LinhaTreino> treino, IReadOnlyList<float> probabilidades)
    {
        var vetores = treino.Select(linha => linha.Features.ParaArray()).ToList();
        var distribuicao = VetorFeatures.Nomes
            .Select((nome, indice) => CalculadoraDistribuicao.Calcular(nome, vetores.Select(vetor => (double)vetor[indice]).ToList()))
            .ToList();
        distribuicao.Add(CalculadoraDistribuicao.Calcular(DistribuicaoFeature.NomeScore, probabilidades.Select(valor => (double)valor).ToList()));
        return distribuicao;
    }

    private static void ExigirAsDuasClasses(IReadOnlyList<LinhaTreino> linhas, string conjunto)
    {
        if (!linhas.Any(linha => linha.Fraude) || linhas.All(linha => linha.Fraude))
        {
            throw new InvalidOperationException($"O conjunto de {conjunto} precisa de fraudes e de operações normais.");
        }
    }
}
