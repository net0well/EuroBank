using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Uma versão treinada do modelo de fraude: o arquivo do ML.NET, as métricas no teste temporal, o limiar recomendado,
/// de onde vieram os dados e a distribuição do treino (a referência do drift). Nasce Registrada; o analista a coloca
/// em sombra (Challenger) ou a ativa. Ativar uma versão aposenta a ativa anterior (quem garante uma só é a aplicação,
/// com um índice único no banco por trás).
/// </summary>
public sealed class VersaoModelo
{
    private List<DistribuicaoFeature> _distribuicao = [];

    private VersaoModelo()
    {
        // Usado pelo EF Core.
    }

    /// <summary>Número sequencial, dado pelo banco.</summary>
    public int Versao { get; private set; }

    public StatusModelo Status { get; private set; }

    /// <summary>O modelo salvo pelo ML.NET (zip com o pipeline e o esquema de entrada).</summary>
    public byte[] Arquivo { get; private set; } = [];

    public MetricasModelo Metricas { get; private set; } = null!;

    /// <summary>A probabilidade a partir da qual o treino recomenda tratar a operação como suspeita.</summary>
    public double LimiarRecomendado { get; private set; }

    public DadosTreino Dados { get; private set; } = null!;

    public IReadOnlyList<DistribuicaoFeature> Distribuicao => _distribuicao.AsReadOnly();

    public DateTimeOffset TreinadaEm { get; private set; }

    public DateTimeOffset? AtivadaEm { get; private set; }

    public DateTimeOffset? AposentadaEm { get; private set; }

    public static VersaoModelo Registrar(
        byte[] arquivo,
        MetricasModelo metricas,
        double limiarRecomendado,
        DadosTreino dados,
        IReadOnlyList<DistribuicaoFeature> distribuicao,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        ArgumentNullException.ThrowIfNull(distribuicao);
        if (arquivo.Length == 0)
        {
            throw new ArgumentException("O arquivo do modelo está vazio.", nameof(arquivo));
        }

        return new VersaoModelo
        {
            Status = StatusModelo.Registrada,
            Arquivo = arquivo,
            Metricas = metricas,
            LimiarRecomendado = limiarRecomendado,
            Dados = dados,
            _distribuicao = [.. distribuicao],
            TreinadaEm = agora,
        };
    }

    /// <summary>Passa a pontuar em sombra. Já em sombra: nada muda.</summary>
    /// <exception cref="ExcecaoTransicaoModeloInvalida">A versão está ativa.</exception>
    public void ColocarEmSombra()
    {
        if (Status == StatusModelo.Challenger)
        {
            return;
        }

        if (Status == StatusModelo.Ativa)
        {
            throw new ExcecaoTransicaoModeloInvalida(Versao, Status, StatusModelo.Challenger);
        }

        Status = StatusModelo.Challenger;
        AposentadaEm = null;
    }

    /// <summary>Passa a decidir. Já ativa: nada muda.</summary>
    public void Ativar(DateTimeOffset agora)
    {
        if (Status == StatusModelo.Ativa)
        {
            return;
        }

        Status = StatusModelo.Ativa;
        AtivadaEm = agora;
        AposentadaEm = null;
    }

    /// <summary>Sai de cena (outra foi ativada, ou o challenger foi retirado).</summary>
    public void Aposentar(DateTimeOffset agora)
    {
        if (Status == StatusModelo.Aposentada)
        {
            return;
        }

        Status = StatusModelo.Aposentada;
        AposentadaEm = agora;
    }
}
