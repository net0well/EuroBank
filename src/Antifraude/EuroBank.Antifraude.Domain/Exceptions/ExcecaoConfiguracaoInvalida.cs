using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Limiares fora de 1 a 100, revisão não menor que recusa, ou peso do modelo fora de 0 a 100.</summary>
public sealed class ExcecaoConfiguracaoInvalida(string motivo)
    : ExcecaoValidacao("configuracao_invalida", $"Configuração de decisão inválida: {motivo}.");
