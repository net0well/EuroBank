using EuroBank.Shared.Enums;

namespace EuroBank.Shared.Exceptions;

/// <summary>Operação entre valores de moedas diferentes: conversão só pelo câmbio, nunca implícita.</summary>
public sealed class ExcecaoMoedaDiferente(Moeda esperada, Moeda recebida)
    : ExcecaoRegraNegocio("moeda_diferente", $"Operação em {recebida} numa conta ou valor em {esperada}.");
