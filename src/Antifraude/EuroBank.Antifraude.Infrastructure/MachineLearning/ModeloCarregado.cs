using System.Collections.Concurrent;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using Microsoft.ML;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Uma versão do modelo em memória: a cadeia salva pelo treino (modelo e contribuições das features) e um pool de
/// PredictionEngine, que não pode ser usado por duas threads ao mesmo tempo. Ao carregar, uma previsão de teste
/// confere que o arquivo tem o formato que a produção espera.
/// </summary>
public sealed class ModeloCarregado
{
    private static readonly int TamanhoPool = Environment.ProcessorCount * 2;

    private readonly MLContext _ml;
    private readonly ITransformer _modelo;
    private readonly ConcurrentBag<PredictionEngine<EntradaModelo, SaidaModelo>> _motores = [];

    private ModeloCarregado(int versao, MLContext ml, ITransformer modelo)
    {
        Versao = versao;
        _ml = ml;
        _modelo = modelo;
    }

    public int Versao { get; }

    /// <exception cref="InvalidDataException">O arquivo não é um modelo deste formato.</exception>
    public static ModeloCarregado Carregar(int versao, byte[] arquivo)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        var ml = new MLContext();
        ITransformer modelo;
        try
        {
            using var conteudo = new MemoryStream(arquivo);
            modelo = ml.Model.Load(conteudo, out _);
        }
        catch (Exception erro) when (erro is not OutOfMemoryException)
        {
            throw new InvalidDataException($"O arquivo da versão {versao} não é um modelo do ML.NET.", erro);
        }

        var carregado = new ModeloCarregado(versao, ml, modelo);
        carregado.Pontuar(new VetorFeatures(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0));
        return carregado;
    }

    /// <exception cref="InvalidDataException">A saída do modelo não é uma probabilidade com uma contribuição por feature.</exception>
    public PontuacaoModelo Pontuar(VetorFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        var motor = _motores.TryTake(out var livre) ? livre : _ml.Model.CreatePredictionEngine<EntradaModelo, SaidaModelo>(_modelo);
        try
        {
            var saida = motor.Predict(EntradaModelo.De(features));
            if (float.IsNaN(saida.Probability) || saida.FeatureContributions.Length != VetorFeatures.Nomes.Count)
            {
                throw new InvalidDataException($"A versão {Versao} devolveu uma saída fora do formato esperado.");
            }

            return new PontuacaoModelo(
                Versao,
                saida.Probability,
                VetorFeatures.Nomes.Zip(saida.FeatureContributions, (nome, valor) => new ContribuicaoFeature(nome, valor)).ToList());
        }
        finally
        {
            if (_motores.Count < TamanhoPool)
            {
                _motores.Add(motor);
            }
        }
    }
}
