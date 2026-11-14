namespace EuroBank.Antifraude.Domain.Enums;

public enum OrigemDecisao
{
    /// <summary>O motor decidiu na hora (aprovação ou recusa).</summary>
    Motor = 1,

    Analista = 2,

    /// <summary>O caso de revisão passou do prazo.</summary>
    Expiracao = 3,

    /// <summary>O cliente cancelou a operação durante a revisão.</summary>
    Cliente = 4,
}
