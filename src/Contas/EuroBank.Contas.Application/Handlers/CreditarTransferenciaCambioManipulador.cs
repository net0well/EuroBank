using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

/// <summary>Crédito na conta de destino; conta encerrada responde CreditoRecusado e a saga estorna a origem.</summary>
public sealed class CreditarTransferenciaCambioManipulador(PassoDaSaga passo, IRespostasSaga respostas, TimeProvider relogio)
{
    public Task Handle(CreditarTransferenciaCambio comando, CancellationToken cancellationToken) =>
        passo.ExecutarAsync(
            comando.TransferenciaId,
            comando.ContaId,
            comando.Moeda,
            conta => conta.CreditarTransferenciaCambio(
                comando.TransferenciaId,
                Dinheiro.De(comando.Valor, conta.Moeda),
                comando.IbanOrigem,
                comando.NomeOrigem,
                comando.Descricao,
                Conversao.Criar(Dinheiro.De(comando.ValorOrigem, Moedas.Ler(comando.MoedaOrigem)), comando.Taxa),
                relogio.GetUtcNow()),
            transferencia => respostas.CreditoRealizado(comando.TransferenciaId, comando.ContaId, transferencia.Valor),
            motivo => respostas.CreditoRecusado(comando.TransferenciaId, comando.ContaId, motivo),
            cancellationToken);
}
