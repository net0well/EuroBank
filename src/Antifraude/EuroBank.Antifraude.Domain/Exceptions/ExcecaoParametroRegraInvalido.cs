using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Peso fora de 0 a 100, parâmetro desconhecido ou não positivo, ou peso em regra dura.</summary>
public sealed class ExcecaoParametroRegraInvalido(string motivo)
    : ExcecaoValidacao("parametro_regra_invalido", $"Ajuste de regra inválido: {motivo}.");
