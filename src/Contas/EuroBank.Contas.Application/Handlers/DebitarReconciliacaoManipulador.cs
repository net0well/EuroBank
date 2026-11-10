using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Services;

namespace EuroBank.Contas.Application.Handlers;

public sealed class DebitarReconciliacaoManipulador(PassoDaSaga passo, IRespostasSaga respostas, TimeProvider relogio)
{
    public Task Handle(DebitarReconciliacao comando, CancellationToken cancellationToken) =>
        passo.ExecutarAsync(
            comando.TransferenciaId,
            comando.ContaId,
            moedaEsperada: null,
            conta => conta.DebitarReconciliacao(comando.TransferenciaId, relogio.GetUtcNow()),
            transferencia => respostas.ReconciliacaoRealizada(
                comando.TransferenciaId, comando.ContaId, transferencia.Valor + transferencia.Tarifa),
            motivo => respostas.ReconciliacaoRecusada(comando.TransferenciaId, comando.ContaId, motivo),
            cancellationToken);
}
