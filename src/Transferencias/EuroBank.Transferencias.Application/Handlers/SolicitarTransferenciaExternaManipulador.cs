using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Transferência para um IBAN de outro banco; a moeda da conta vem da projeção local de contas.</summary>
public sealed class SolicitarTransferenciaExternaManipulador(SolicitacaoDeTransferencia solicitacao, IContasConhecidas contas)
{
    public Task<TransferenciaDto> Handle(SolicitarTransferenciaExterna comando, CancellationToken cancellationToken)
    {
        var hash = ImpressaoDigitalPedido.Calcular(comando);
        return solicitacao.RegistrarAsync(
            comando.IdempotencyKey,
            hash,
            async (chave, agora) =>
            {
                var origem = await contas.ObterPorIdAsync(comando.ContaOrigemId, cancellationToken)
                    ?? throw new ExcecaoContaOrigemDesconhecida(comando.ContaOrigemId);
                var moedaDestino = LerOu(comando.MoedaDestino, origem.Moeda);

                return Transferencia.SolicitarExterna(
                    origem,
                    IbanDestino.Criar(comando.IbanDestino),
                    comando.Bic,
                    comando.NomeBeneficiario,
                    Dinheiro.De(comando.Valor, LerOu(comando.MoedaDoValor, origem.Moeda)),
                    moedaDestino,
                    comando.Descricao,
                    comando.EmailNotificacao,
                    chave,
                    hash,
                    comando.ExecutarEm,
                    agora);
            },
            cancellationToken);
    }

    private static Moeda LerOu(string? codigo, Moeda padrao) =>
        string.IsNullOrWhiteSpace(codigo) ? padrao : Moedas.Ler(codigo);
}
