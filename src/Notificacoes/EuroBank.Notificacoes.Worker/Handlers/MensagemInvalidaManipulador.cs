using EuroBank.Shared.Exceptions;
using EuroBank.Shared.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Handlers;

/// <summary>Falha de propósito: a política de erros do Wolverine move a mensagem direto para o dead letter.</summary>
public sealed class MensagemInvalidaManipulador
{
    public void Handle(MensagemInvalida mensagem) => throw new ExcecaoMensagemInvalida(mensagem.Motivo);
}
