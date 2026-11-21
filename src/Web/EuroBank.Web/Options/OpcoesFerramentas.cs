namespace EuroBank.Web.Options;

/// <summary>
/// Seção "Ferramentas": os endereços, vistos do navegador, das ferramentas do laboratório que o início lista. O compose
/// troca os que têm porta configurável no .env.
/// </summary>
public sealed class OpcoesFerramentas
{
    public const string Secao = "Ferramentas";

    public Uri KafbatUi { get; set; } = new("http://localhost:8080");

    public Uri Grafana { get; set; } = new("http://localhost:3000");

    public Uri Prometheus { get; set; } = new("http://localhost:9090");

    public Uri Jaeger { get; set; } = new("http://localhost:16686");

    public Uri Mailpit { get; set; } = new("http://localhost:8025");

    public Uri RedisInsight { get; set; } = new("http://localhost:5540");
}
