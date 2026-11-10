using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Replies;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Respostas do câmbio simulado: a cotação (que decide o débito) ou a indisponibilidade do par.</summary>
public sealed class RespostasCambioManipulador(AplicadorRespostas aplicador, PoliticaSaga politica)
{
    public Task Handle(CotacaoEmitida resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarComLimiteAsync(
            resposta.TransferenciaId,
            nameof(CotacaoEmitida),
            (transferencia, jaTransferido, agora) => transferencia.RegistrarCotacaoEmitida(
                new CotacaoTravada(resposta.CotacaoId, resposta.Taxa, resposta.ValidaAte),
                Dinheiro.De(resposta.ValorOrigem, Moedas.Ler(resposta.MoedaOrigem)),
                Dinheiro.De(resposta.ValorDestino, Moedas.Ler(resposta.MoedaDestino)),
                jaTransferido,
                politica,
                agora),
            cancellationToken);

    public Task Handle(CotacaoIndisponivel resposta, CancellationToken cancellationToken) =>
        aplicador.AplicarAsync(
            resposta.TransferenciaId,
            nameof(CotacaoIndisponivel),
            (transferencia, agora) => transferencia.RegistrarCotacaoIndisponivel(resposta.Motivo, agora),
            cancellationToken);
}
