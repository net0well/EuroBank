using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Application.Mappers;

public static class ModeloMapeador
{
    /// <summary>As contribuições da maior para a menor (em valor absoluto), com quatro casas.</summary>
    public static PontuacaoModeloDto ParaDto(this PontuacaoModelo pontuacao) =>
        new(
            pontuacao.Versao,
            Math.Round(pontuacao.Probabilidade, 4),
            pontuacao.Pontos,
            pontuacao.Contribuicoes
                .OrderByDescending(contribuicao => Math.Abs(contribuicao.Valor))
                .Select(contribuicao => new ContribuicaoFeatureDto(contribuicao.Feature, Math.Round(contribuicao.Valor, 4)))
                .ToList());

    /// <summary>As features pelo nome, na ordem do vetor.</summary>
    public static IReadOnlyDictionary<string, double> ParaDicionario(this VetorFeatures features) =>
        VetorFeatures.Nomes
            .Zip(features.ParaArray(), (nome, valor) => (Nome: nome, Valor: valor))
            .ToDictionary(par => par.Nome, par => Math.Round(par.Valor, 4));

    public static VersaoModeloResumoDto ParaResumo(this VersaoModelo versao) =>
        new(
            versao.Versao,
            versao.Status.ToString(),
            versao.Metricas.AucPr,
            versao.Metricas.Precisao,
            versao.Metricas.Recall,
            versao.Metricas.F1,
            versao.LimiarRecomendado,
            versao.Dados.MetaAtingida,
            versao.TreinadaEm,
            versao.AtivadaEm,
            versao.AposentadaEm);
}
