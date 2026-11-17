using System.Globalization;

namespace EuroBank.Antifraude.Treinamento;

/// <summary>Os argumentos da linha de comando: o comando e as opções no formato --nome valor.</summary>
public sealed class Argumentos
{
    private readonly Dictionary<string, string> _opcoes;

    private Argumentos(string comando, Dictionary<string, string> opcoes)
    {
        Comando = comando;
        _opcoes = opcoes;
    }

    public string Comando { get; }

    public static Argumentos Ler(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var opcoes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < args.Length - 1; i += 2)
        {
            opcoes[args[i].TrimStart('-')] = args[i + 1];
        }

        return new Argumentos(args.Length > 0 ? args[0] : string.Empty, opcoes);
    }

    public string Texto(string nome, string padrao) => _opcoes.GetValueOrDefault(nome, padrao);

    public int Inteiro(string nome, int padrao) =>
        _opcoes.TryGetValue(nome, out var valor) ? int.Parse(valor, CultureInfo.InvariantCulture) : padrao;

    public double Real(string nome, double padrao) =>
        _opcoes.TryGetValue(nome, out var valor) ? double.Parse(valor, CultureInfo.InvariantCulture) : padrao;
}
