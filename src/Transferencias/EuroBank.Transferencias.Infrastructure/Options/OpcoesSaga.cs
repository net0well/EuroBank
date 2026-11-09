using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Infrastructure.Options;

/// <summary>Seção "Saga" do appsettings (prazos no formato hh:mm:ss). Os testes ponta a ponta usam prazos curtos.</summary>
public sealed class OpcoesSaga
{
    public const string Secao = "Saga";

    public TimeSpan PrazoDebito { get; set; } = PoliticaSaga.Padrao.PrazoDebito;

    public TimeSpan PrazoLiquidacao { get; set; } = PoliticaSaga.Padrao.PrazoLiquidacao;

    public TimeSpan PrazoConsulta { get; set; } = PoliticaSaga.Padrao.PrazoConsulta;

    public TimeSpan PrazoEstorno { get; set; } = PoliticaSaga.Padrao.PrazoEstorno;

    public int ReenviosPorPasso { get; set; } = PoliticaSaga.Padrao.ReenviosPorPasso;

    public PoliticaSaga ParaPolitica() => new()
    {
        PrazoDebito = PrazoDebito,
        PrazoLiquidacao = PrazoLiquidacao,
        PrazoConsulta = PrazoConsulta,
        PrazoEstorno = PrazoEstorno,
        ReenviosPorPasso = ReenviosPorPasso,
    };
}
