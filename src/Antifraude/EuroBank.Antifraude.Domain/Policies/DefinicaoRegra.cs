using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.Policies;

/// <summary>Como uma regra nasce: tipo, descrição, peso e parâmetros padrão (os únicos que ela aceita).</summary>
public sealed record DefinicaoRegra(
    CodigoRegra Codigo,
    TipoRegra Tipo,
    string Descricao,
    int PesoPadrao,
    EfeitoRegraDura? Efeito,
    IReadOnlyDictionary<string, decimal> ParametrosPadrao);
