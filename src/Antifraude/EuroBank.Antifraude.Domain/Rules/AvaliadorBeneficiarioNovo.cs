using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

public sealed class AvaliadorBeneficiarioNovo : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.BeneficiarioNovo;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (NormalizacaoIban.Normalizar(operacao.IbanDestino) is not { } iban)
        {
            return ResultadoRegra.Nao("Operação sem beneficiário");
        }

        return perfil.BeneficiariosConhecidos.Contains(iban)
            ? ResultadoRegra.Nao("Beneficiário já usado pela conta")
            : ResultadoRegra.Sim($"Primeira transferência da conta para {iban}");
    }
}
