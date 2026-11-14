using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Policies;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Uma regra do motor como está configurada agora. Ajustes valem para a próxima avaliação, sem reiniciar. Em modo
/// sombra a regra é avaliada e aparece no detalhe da análise, mas não soma pontos nem decide.
/// </summary>
public sealed class Regra
{
    private Dictionary<string, decimal> _parametros = [];

    private Regra()
    {
        // Usado pelo EF Core.
    }

    public CodigoRegra Codigo { get; private set; }

    public TipoRegra Tipo { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    /// <summary>Pontos somados quando dispara (0 nas regras duras).</summary>
    public int Peso { get; private set; }

    /// <summary>O que uma regra dura faz quando dispara.</summary>
    public EfeitoRegraDura? Efeito { get; private set; }

    public bool Ativa { get; private set; }

    public bool Sombra { get; private set; }

    public IReadOnlyDictionary<string, decimal> Parametros => _parametros.AsReadOnly();

    public DateTimeOffset AtualizadaEm { get; private set; }

    public static Regra Criar(DefinicaoRegra definicao, DateTimeOffset agora) =>
        new()
        {
            Codigo = definicao.Codigo,
            Tipo = definicao.Tipo,
            Descricao = definicao.Descricao,
            Peso = definicao.PesoPadrao,
            Efeito = definicao.Efeito,
            Ativa = true,
            Sombra = false,
            _parametros = new Dictionary<string, decimal>(definicao.ParametrosPadrao, StringComparer.Ordinal),
            AtualizadaEm = agora,
        };

    public decimal Parametro(string nome) =>
        _parametros.TryGetValue(nome, out var valor)
            ? valor
            : throw new InvalidOperationException($"A regra {Codigo} não tem o parâmetro {nome}.");

    /// <param name="parametros">Só os parâmetros que mudam; os demais ficam como estão. Nulo: nenhum muda.</param>
    public void Ajustar(int peso, bool ativa, bool sombra, IReadOnlyDictionary<string, decimal>? parametros, DateTimeOffset agora)
    {
        if (Tipo == TipoRegra.Dura && peso != 0)
        {
            throw new ExcecaoParametroRegraInvalido("regra dura não tem peso (use 0)");
        }

        if (peso is < 0 or > 100)
        {
            throw new ExcecaoParametroRegraInvalido("o peso deve estar entre 0 e 100");
        }

        foreach (var (nome, valor) in parametros ?? new Dictionary<string, decimal>())
        {
            if (!_parametros.ContainsKey(nome))
            {
                throw new ExcecaoParametroRegraInvalido($"a regra {Codigo} não tem o parâmetro {nome}");
            }

            if (valor <= 0)
            {
                throw new ExcecaoParametroRegraInvalido($"o parâmetro {nome} deve ser maior que zero");
            }
        }

        foreach (var (nome, valor) in parametros ?? new Dictionary<string, decimal>())
        {
            _parametros[nome] = valor;
        }

        Peso = peso;
        Ativa = ativa;
        Sombra = sombra;
        AtualizadaEm = agora;
    }
}
