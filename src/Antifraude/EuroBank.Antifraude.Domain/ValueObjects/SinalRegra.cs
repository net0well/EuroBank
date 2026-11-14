using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// O que uma regra disse sobre a operação. Pontos é o peso quando uma regra com peso dispara (inclusive em modo
/// sombra, para mostrar quanto ela somaria); regra em sombra aparece no detalhe, mas não entra no score nem decide.
/// </summary>
public sealed record SinalRegra(
    CodigoRegra Codigo,
    TipoRegra Tipo,
    bool Disparou,
    int Pontos,
    bool Sombra,
    string Motivo,
    EfeitoRegraDura? Efeito);
