using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Avalia uma regra com os parâmetros configurados; não decide nem pontua (isso é do motor).</summary>
public interface IAvaliadorRegra
{
    CodigoRegra Codigo { get; }

    ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra);
}
