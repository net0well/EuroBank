using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Application.Mappers;

public static class AdministracaoMapeador
{
    public static RegraDto ParaDto(this Regra regra) =>
        new(
            regra.Codigo.ToString(),
            regra.Tipo.ToString(),
            regra.Descricao,
            regra.Peso,
            regra.Efeito?.ToString(),
            regra.Ativa,
            regra.Sombra,
            regra.Parametros,
            regra.AtualizadaEm);

    public static ConfiguracaoDecisaoDto ParaDto(this ConfiguracaoDecisao configuracao) =>
        new(configuracao.Versao, configuracao.LimiarRevisao, configuracao.LimiarRecusa, configuracao.PesoModelo, configuracao.CriadaEm);

    public static IbanBloqueadoDto ParaDto(this IbanBloqueado iban) =>
        new(iban.Iban, iban.Motivo, iban.BloqueadoPor, iban.BloqueadoEm);

    public static PaisRiscoDto ParaDto(this PaisRisco pais) => new(pais.Codigo, pais.IncluidoEm);

    public static FraudeConfirmadaDto ParaDto(this FraudeConfirmada fraude) =>
        new(fraude.Id, fraude.OperacaoId, fraude.AnaliseId, fraude.Analista, fraude.Observacao, fraude.ConfirmadaEm);
}
