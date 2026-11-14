using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Application.Exceptions;

/// <summary>Pedido de avaliação que não dá para entender (id que não é Guid, tipo ou moeda desconhecidos, sem valor).</summary>
public sealed class ExcecaoPedidoAvaliacaoInvalido(string motivo)
    : ExcecaoValidacao("pedido_avaliacao_invalido", $"Pedido de avaliação inválido: {motivo}.");
