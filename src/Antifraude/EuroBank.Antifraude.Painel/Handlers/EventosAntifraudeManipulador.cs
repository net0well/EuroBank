using EuroBank.Antifraude.Painel.Services;
using EuroBank.Antifraude.Painel.TransferObjects;

namespace EuroBank.Antifraude.Painel.Handlers;

/// <summary>
/// Os eventos do antifraude que mexem na fila viram avisos para as telas abertas. As decisões do motor ficam de fora:
/// não têm caso, e sob carga fariam a fila recarregar sem parar.
/// </summary>
public sealed class EventosAntifraudeManipulador(AvisosFila avisos)
{
    public void Handle(EventoCasoAberto evento) =>
        avisos.Publicar(new AvisoFila(evento.OperacaoId, evento.CasoId, evento.ScoreFinal, Decisao: null, Origem: null, Analista: null));

    public void Handle(EventoAnaliseDecidida evento)
    {
        if (evento.Origem == "Motor")
        {
            return;
        }

        avisos.Publicar(new AvisoFila(evento.OperacaoId, CasoId: null, evento.ScoreFinal, evento.Decisao, evento.Origem, evento.Analista));
    }
}
