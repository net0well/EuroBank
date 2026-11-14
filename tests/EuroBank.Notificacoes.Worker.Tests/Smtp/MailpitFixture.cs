using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace EuroBank.Notificacoes.Worker.Tests.Smtp;

/// <summary>Mailpit de verdade: SMTP na 1025 e API HTTP na 8025, portas aleatórias no host.</summary>
public sealed class MailpitFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:v1.33.1")
        .WithPortBinding(1025, assignRandomHostPort: true)
        .WithPortBinding(8025, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(requisicao => requisicao.ForPort(8025).ForPath("/readyz")))
        .Build();

    public string Host => _container.Hostname;

    public int PortaSmtp => _container.GetMappedPublicPort(1025);

    public HttpClient Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Api = new HttpClient { BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(8025)}") };
    }

    /// <summary>Mensagens recebidas para o destinatário (resumo da listagem do Mailpit).</summary>
    public async Task<List<JsonElement>> MensagensParaAsync(string destinatario)
    {
        var busca = await Api.GetFromJsonAsync<JsonElement>(
            $"/api/v1/search?query={Uri.EscapeDataString($"to:{destinatario}")}", TestContext.Current.CancellationToken);
        return busca.GetProperty("messages").EnumerateArray().ToList();
    }

    /// <summary>Mensagem completa (com HTML, texto e Message-ID) pelo id interno do Mailpit.</summary>
    public Task<JsonElement> MensagemAsync(string id) =>
        Api.GetFromJsonAsync<JsonElement>($"/api/v1/message/{id}", TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        Api?.Dispose();
        await _container.DisposeAsync();
    }
}
