namespace EuroBank.Contas.Infrastructure.Options;

/// <summary>
/// Seção "Antifraude": se o saque e a transferência interna passam pelo antifraude (desligado fora do compose, como a
/// análise de risco da saga), o endereço gRPC (h2c), a API key e o prazo da chamada.
/// </summary>
public sealed class OpcoesAntifraude
{
    public const string Secao = "Antifraude";

    /// <summary>Nome do cliente no IHttpClientFactory (os testes ponta a ponta trocam o handler por este nome).</summary>
    public const string NomeCliente = "antifraude";

    public bool Ativo { get; set; }

    public Uri Endereco { get; set; } = new("http://localhost:5410");

    /// <summary>Vai no metadata x-api-key de toda chamada.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Tempo total da chamada, as novas tentativas incluídas: é quanto o cliente espera a resposta do saque.</summary>
    public TimeSpan Deadline { get; set; } = TimeSpan.FromSeconds(2);
}
