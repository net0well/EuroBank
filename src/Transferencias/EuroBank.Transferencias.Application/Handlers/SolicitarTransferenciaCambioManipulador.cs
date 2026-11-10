using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Câmbio entre contas EuroBank: as duas contas, e as moedas delas, vêm da projeção local de contas.</summary>
public sealed class SolicitarTransferenciaCambioManipulador(SolicitacaoDeTransferencia solicitacao, IContasConhecidas contas)
{
    public Task<TransferenciaDto> Handle(SolicitarTransferenciaCambio comando, CancellationToken cancellationToken)
    {
        var hash = ImpressaoDigitalPedido.Calcular(comando);
        return solicitacao.RegistrarAsync(
            comando.IdempotencyKey,
            hash,
            async (chave, agora) =>
            {
                var origem = await contas.ObterPorIdAsync(comando.ContaOrigemId, cancellationToken)
                    ?? throw new ExcecaoContaOrigemDesconhecida(comando.ContaOrigemId);
                var iban = IbanDestino.Criar(comando.IbanDestino);
                if (!iban.EhEuroBank)
                {
                    throw new ExcecaoDestinoNaoEuroBank();
                }

                var destino = await contas.ObterPorIbanAsync(iban.Valor, cancellationToken)
                    ?? throw new ExcecaoContaDestinoDesconhecida(iban.Valor);
                var moedaDoValor = string.IsNullOrWhiteSpace(comando.MoedaDoValor) ? origem.Moeda : Moedas.Ler(comando.MoedaDoValor);

                return Transferencia.SolicitarCambioInterno(
                    origem,
                    destino,
                    Dinheiro.De(comando.Valor, moedaDoValor),
                    comando.Descricao,
                    comando.EmailNotificacao,
                    chave,
                    hash,
                    comando.ExecutarEm,
                    agora);
            },
            cancellationToken);
    }
}
