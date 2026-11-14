using EuroBank.Transferencias.Infrastructure.Enums;

namespace EuroBank.Transferencias.Infrastructure.Options;

/// <summary>Seção "Antifraude" do appsettings: o endereço gRPC (h2c), a API key e a resiliência da chamada.</summary>
public sealed class OpcoesAntifraude
{
    public const string Secao = "Antifraude";

    public Uri Endereco { get; set; } = new("http://localhost:5410");

    /// <summary>Vai no metadata x-api-key de toda chamada.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Tempo total da chamada, tentativas incluídas: é quanto a linha da saga fica travada esperando.</summary>
    public TimeSpan Deadline { get; set; } = TimeSpan.FromSeconds(2);

    public EstrategiaChamadaAntifraude Estrategia { get; set; } = EstrategiaChamadaAntifraude.Retry;

    /// <summary>Tentativas por chamada no retry e no hedging, contando a primeira.</summary>
    public int MaxTentativas { get; set; } = 3;
}
