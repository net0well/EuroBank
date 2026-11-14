using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

public sealed class AvaliadorPaisDeRisco : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.PaisDeRisco;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (operacao.PaisDestino is not { } pais)
        {
            return ResultadoRegra.Nao("Operação sem país de destino");
        }

        return listas.EhPaisDeRisco(pais)
            ? ResultadoRegra.Sim($"País do IBAN de destino ({pais}) na lista de países de risco")
            : ResultadoRegra.Nao($"País do IBAN de destino ({pais}) fora da lista de risco");
    }
}
