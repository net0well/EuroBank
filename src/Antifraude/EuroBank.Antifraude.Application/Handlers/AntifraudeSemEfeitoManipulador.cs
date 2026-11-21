using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Consome e descarta: sem isso, cada CasoAberto viraria um alerta de "tipo desconhecido".</summary>
public sealed class AntifraudeSemEfeitoManipulador
{
    public static void Handle(EventoAntifraudeSemEfeito evento)
    {
    }
}
