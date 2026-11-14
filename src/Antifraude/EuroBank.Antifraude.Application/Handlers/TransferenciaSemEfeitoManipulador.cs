using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Consome e descarta: sem isso, cada evento conhecido viraria um alerta de "tipo desconhecido".</summary>
public sealed class TransferenciaSemEfeitoManipulador
{
    public static void Handle(EventoTransferenciaSemEfeito evento)
    {
    }
}
