namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>As regras do motor. Dispositivo e localização (viagem impossível, dispositivo novo, país) vêm depois.</summary>
public enum CodigoRegra
{
    ListaBloqueio = 1,
    ValorAcimaDaMedia = 2,
    Velocidade = 3,
    Esvaziamento = 4,
    BeneficiarioNovo = 5,
    TesteDeCartao = 6,
    ContaRecemAberta = 7,
    HorarioIncomum = 8,
    PaisDeRisco = 9,
}
