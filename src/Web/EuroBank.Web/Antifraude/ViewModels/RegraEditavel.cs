using EuroBank.Web.Antifraude.TransferObjects;

namespace EuroBank.Web.Antifraude.ViewModels;

/// <summary>Uma regra na tela de regras: o que o analista está editando até salvar.</summary>
public sealed class RegraEditavel
{
    private RegraEditavel(Regra regra)
    {
        Original = regra;
        Peso = regra.Peso;
        Ativa = regra.Ativa;
        Sombra = regra.Sombra;
        Parametros = new Dictionary<string, decimal>(regra.Parametros, StringComparer.Ordinal);
    }

    public Regra Original { get; }

    public int Peso { get; set; }

    public bool Ativa { get; set; }

    public bool Sombra { get; set; }

    public Dictionary<string, decimal> Parametros { get; }

    /// <summary>Regra dura: decide sozinha, sem peso.</summary>
    public bool Dura => Original.Tipo == "Dura";

    public bool Alterada =>
        Peso != Original.Peso || Ativa != Original.Ativa || Sombra != Original.Sombra
        || Parametros.Any(parametro => Original.Parametros[parametro.Key] != parametro.Value);

    public static RegraEditavel De(Regra regra) => new(regra);

    /// <summary>Só os parâmetros que mudaram vão no ajuste.</summary>
    public AjusteRegraRequisicao ParaAjuste() =>
        new(Peso, Ativa, Sombra, Parametros.Where(parametro => Original.Parametros[parametro.Key] != parametro.Value)
            .ToDictionary(parametro => parametro.Key, parametro => parametro.Value));
}
