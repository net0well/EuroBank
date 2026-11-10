using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Domain.Exceptions;

public sealed class ExcecaoTransferenciaEntreMoedas(Moeda origem, Moeda destino)
    : ExcecaoRegraNegocio(
        "transferencia_entre_moedas",
        $"A conta de origem é em {origem} e a de destino em {destino}: use POST /transferencias-cambio.");
