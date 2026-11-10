using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Infrastructure.Options;

/// <summary>Seção "Sepa" do appsettings: o horário de corte das transferências em EUR.</summary>
public sealed class OpcoesSepa
{
    public const string Secao = "Sepa";

    public bool AplicarHorarioDeCorte { get; set; } = PoliticaSepa.Padrao.AplicarHorarioDeCorte;

    public TimeOnly HorarioDeCorte { get; set; } = PoliticaSepa.Padrao.HorarioDeCorte;

    public TimeOnly Abertura { get; set; } = PoliticaSepa.Padrao.Abertura;

    public PoliticaSepa ParaPolitica() => new()
    {
        AplicarHorarioDeCorte = AplicarHorarioDeCorte,
        HorarioDeCorte = HorarioDeCorte,
        Abertura = Abertura,
    };
}
