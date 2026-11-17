namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>
/// O ciclo de vida de uma versão do modelo. Só uma fica Ativa (decide, com o peso da configuração) e no máximo uma
/// fica Challenger (pontua em sombra, sem decidir). Aposentada sai de cena, mas continua registrada.
/// </summary>
public enum StatusModelo
{
    Registrada = 1,
    Challenger = 2,
    Ativa = 3,
    Aposentada = 4,
}
