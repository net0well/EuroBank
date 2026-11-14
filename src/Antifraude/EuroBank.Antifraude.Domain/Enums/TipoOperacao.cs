namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>Toda saída de dinheiro do EuroBank passa pelo antifraude.</summary>
public enum TipoOperacao
{
    Externa = 1,
    Cambio = 2,
    Saque = 3,
    TransferenciaInterna = 4,
}
