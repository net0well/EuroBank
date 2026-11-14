using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

public sealed class AvaliadorListaBloqueio : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.ListaBloqueio;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra) =>
        listas.EstaBloqueado(operacao.IbanDestino)
            ? ResultadoRegra.Sim($"IBAN {operacao.IbanDestino} na lista de bloqueio")
            : ResultadoRegra.Nao("IBAN de destino fora da lista de bloqueio");
}
