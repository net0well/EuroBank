using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoMoedaDoValorInvalida(Moeda moedaDoValor, Moeda origem, Moeda destino)
    : ExcecaoRegraNegocio(
        "moeda_do_valor_invalida",
        $"O valor em {moedaDoValor} não serve: use a moeda da conta de origem ({origem}) ou a de destino ({destino}).");
