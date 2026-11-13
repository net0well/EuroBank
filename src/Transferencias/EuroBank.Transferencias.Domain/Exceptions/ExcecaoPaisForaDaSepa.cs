using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Domain.Exceptions;

public sealed class ExcecaoPaisForaDaSepa(string pais)
    : ExcecaoRegraNegocio(
        "pais_fora_da_sepa",
        $"Transferência em EUR só para países da área SEPA; {pais} não é. Use outra moeda de destino.");
