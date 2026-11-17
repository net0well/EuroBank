namespace EuroBank.Antifraude.Painel.Tests.Fixtures;

/// <summary>Um pedido que chegou à API falsa: método, caminho com a query e o corpo (nulo sem corpo).</summary>
public sealed record PedidoRecebido(string Metodo, string Caminho, string? Corpo);
