namespace EuroBank.Antifraude.Painel.Options;

/// <summary>Seção "Antifraude": onde está a REST do antifraude e a API key, que fica só no servidor do painel.</summary>
public sealed class OpcoesAntifraude
{
    public const string Secao = "Antifraude";

    public Uri Endereco { get; set; } = new("http://localhost:5400");

    public string ApiKey { get; set; } = string.Empty;
}
