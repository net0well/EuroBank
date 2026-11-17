using EuroBank.Shared.TransferObjects;

namespace EuroBank.Antifraude.Painel.Handlers;

/// <summary>Uma mensagem sem o header tipo vai para o log; sem inbox, não há dead letter no painel.</summary>
public sealed class MensagemInvalidaManipulador(ILogger<MensagemInvalidaManipulador> logger)
{
    public void Handle(MensagemInvalida mensagem) =>
        logger.LogWarning("Mensagem inválida em eurobank.antifraude.eventos: {Motivo}", mensagem.Motivo);
}
