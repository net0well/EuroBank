using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Mappers;

public static class ResultadoAvaliacaoMapeador
{
    public static ResultadoAvaliacaoDto ParaDto(this Analise analise, CasoRevisao? caso) =>
        new(
            analise.Id.Valor,
            analise.OperacaoId,
            analise.Decisao,
            analise.ScoreFinal,
            caso?.Id.Valor,
            analise.Sinais,
            analise.VersaoConfiguracao,
            analise.CriadaEm,
            analise.DecididaEm,
            Simulacao: false,
            analise.Modelo,
            analise.Challenger);

    public static ResultadoAvaliacaoDto ParaSimulacao(this ResultadoMotor resultado, ContextoOperacao operacao, DateTimeOffset agora) =>
        new(
            AnaliseId: null,
            operacao.OperacaoId,
            resultado.Decisao,
            resultado.ScoreFinal,
            CasoId: null,
            resultado.Sinais,
            resultado.VersaoConfiguracao,
            agora,
            DecididaEm: null,
            Simulacao: true,
            resultado.Modelo,
            resultado.Challenger);
}
