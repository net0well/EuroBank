using System.Net.Sockets;

namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>
/// Entre o cliente gRPC do worker de Transferências e o TestServer do antifraude: desligado, a chamada falha como uma
/// conexão recusada (o gRPC a entrega como Unavailable, e a política de retry do canal entra em ação).
/// </summary>
public sealed class InterruptorAntifraude(Func<bool> foraDoAr, HttpMessageHandler servidor) : DelegatingHandler(servidor)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        foraDoAr()
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException(
                "Antifraude fora do ar (simulado pelo teste).", new SocketException((int)SocketError.ConnectionRefused)))
            : base.SendAsync(request, cancellationToken);
}
