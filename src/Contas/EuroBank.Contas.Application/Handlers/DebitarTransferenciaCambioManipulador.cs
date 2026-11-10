using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class DebitarTransferenciaCambioManipulador(PassoDaSaga passo, IRespostasSaga respostas, TimeProvider relogio)
{
    public Task Handle(DebitarTransferenciaCambio comando, CancellationToken cancellationToken) =>
        passo.ExecutarAsync(
            comando.TransferenciaId,
            comando.ContaId,
            comando.Moeda,
            conta => conta.DebitarTransferenciaCambio(
                comando.TransferenciaId,
                Dinheiro.De(comando.Valor, conta.Moeda),
                Dinheiro.De(comando.Tarifa, conta.Moeda),
                comando.IbanDestino,
                comando.NomeDestino,
                comando.Descricao,
                Conversao.Criar(Dinheiro.De(comando.ValorDestino, Moedas.Ler(comando.MoedaDestino)), comando.Taxa),
                relogio.GetUtcNow()),
            transferencia => respostas.DebitoRealizado(comando.TransferenciaId, comando.ContaId, transferencia.Valor, transferencia.Tarifa),
            motivo => respostas.DebitoRecusado(comando.TransferenciaId, comando.ContaId, motivo),
            cancellationToken);
}
