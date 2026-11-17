namespace EuroBank.Contas.Application.Enums;

/// <summary>As operações de Contas que passam pelo antifraude antes de executar.</summary>
public enum TipoOperacaoRisco
{
    Saque = 1,
    TransferenciaInterna = 2,
}
