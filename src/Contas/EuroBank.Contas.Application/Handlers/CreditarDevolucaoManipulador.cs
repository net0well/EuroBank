using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Services;

namespace EuroBank.Contas.Application.Handlers;

public sealed class CreditarDevolucaoManipulador(PassoDaSaga passo, IRespostasSaga respostas, TimeProvider relogio)
{
    public Task Handle(CreditarDevolucao comando, CancellationToken cancellationToken) =>
        passo.ExecutarAsync(
            comando.TransferenciaId,
            comando.ContaId,
            moedaEsperada: null,
            conta => conta.CreditarDevolucao(comando.TransferenciaId, relogio.GetUtcNow()),
            transferencia => respostas.DevolucaoCreditada(comando.TransferenciaId, comando.ContaId, transferencia.Valor),
            motivo => respostas.DevolucaoRecusada(comando.TransferenciaId, comando.ContaId, motivo),
            cancellationToken);
}
