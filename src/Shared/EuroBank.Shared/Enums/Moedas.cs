using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.Enums;

public static class Moedas
{
    /// <summary>Lê o código da moeda informado pelo cliente ou numa mensagem ("usd", " EUR ").</summary>
    /// <exception cref="ExcecaoMoedaInvalida">Código vazio, numérico ou fora da lista.</exception>
    public static Moeda Ler(string? codigo)
    {
        var normalizado = codigo?.Trim();
        return !string.IsNullOrEmpty(normalizado)
            && !int.TryParse(normalizado, out _)
            && Enum.TryParse<Moeda>(normalizado, ignoreCase: true, out var moeda)
            && Enum.IsDefined(moeda)
                ? moeda
                : throw new ExcecaoMoedaInvalida(codigo ?? string.Empty);
    }
}
