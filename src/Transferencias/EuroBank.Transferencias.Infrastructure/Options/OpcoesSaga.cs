using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Infrastructure.Options;

/// <summary>Seção "Saga" do appsettings (prazos no formato hh:mm:ss). Os testes ponta a ponta usam prazos curtos.</summary>
public sealed class OpcoesSaga
{
    public const string Secao = "Saga";

    public TimeSpan PrazoCotacao { get; set; } = PoliticaSaga.Padrao.PrazoCotacao;

    public TimeSpan PrazoDebito { get; set; } = PoliticaSaga.Padrao.PrazoDebito;

    public TimeSpan PrazoLiquidacao { get; set; } = PoliticaSaga.Padrao.PrazoLiquidacao;

    public TimeSpan PrazoConsulta { get; set; } = PoliticaSaga.Padrao.PrazoConsulta;

    public TimeSpan PrazoCredito { get; set; } = PoliticaSaga.Padrao.PrazoCredito;

    public TimeSpan PrazoEstorno { get; set; } = PoliticaSaga.Padrao.PrazoEstorno;

    public TimeSpan PrazoReconciliacao { get; set; } = PoliticaSaga.Padrao.PrazoReconciliacao;

    public TimeSpan PrazoCreditoDevolucao { get; set; } = PoliticaSaga.Padrao.PrazoCreditoDevolucao;

    public int ReenviosPorPasso { get; set; } = PoliticaSaga.Padrao.ReenviosPorPasso;

    /// <summary>Precisa ser igual na API e no worker: os dois começam sagas (a API, as de execução imediata).</summary>
    public bool AnaliseRiscoAtiva { get; set; } = PoliticaSaga.Padrao.AnaliseRiscoAtiva;

    public TimeSpan PrazoAnaliseRisco { get; set; } = PoliticaSaga.Padrao.PrazoAnaliseRisco;

    public TimeSpan IntervaloAnaliseRisco { get; set; } = PoliticaSaga.Padrao.IntervaloAnaliseRisco;

    public TimeSpan IntervaloConsultaRevisao { get; set; } = PoliticaSaga.Padrao.IntervaloConsultaRevisao;

    public PoliticaSaga ParaPolitica() => new()
    {
        PrazoCotacao = PrazoCotacao,
        PrazoDebito = PrazoDebito,
        PrazoLiquidacao = PrazoLiquidacao,
        PrazoConsulta = PrazoConsulta,
        PrazoCredito = PrazoCredito,
        PrazoEstorno = PrazoEstorno,
        PrazoReconciliacao = PrazoReconciliacao,
        PrazoCreditoDevolucao = PrazoCreditoDevolucao,
        ReenviosPorPasso = ReenviosPorPasso,
        AnaliseRiscoAtiva = AnaliseRiscoAtiva,
        PrazoAnaliseRisco = PrazoAnaliseRisco,
        IntervaloAnaliseRisco = IntervaloAnaliseRisco,
        IntervaloConsultaRevisao = IntervaloConsultaRevisao,
    };
}
