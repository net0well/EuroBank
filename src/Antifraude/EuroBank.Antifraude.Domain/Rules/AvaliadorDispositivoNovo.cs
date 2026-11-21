using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>
/// Um dispositivo que a conta nunca usou numa operação aprovada. Conta sem nenhum dispositivo conhecido não dispara: não
/// há referência (a conta recém-aberta tem a regra própria).
/// </summary>
public sealed class AvaliadorDispositivoNovo : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.DispositivoNovo;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (operacao.DispositivoId is not { } dispositivo)
        {
            return ResultadoRegra.Nao("Operação sem dispositivo informado");
        }

        if (perfil.DispositivosConhecidos.Count == 0)
        {
            return ResultadoRegra.Nao("Conta sem dispositivo conhecido: ainda não há referência");
        }

        return perfil.DispositivosConhecidos.Contains(dispositivo)
            ? ResultadoRegra.Nao("Dispositivo já usado pela conta")
            : ResultadoRegra.Sim($"Dispositivo {dispositivo} nunca usado pela conta ({perfil.DispositivosConhecidos.Count} conhecido(s))");
    }
}
