using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class DebitarTransferenciaExternaManipulador(PassoDaSaga passo, IRespostasSaga respostas, TimeProvider relogio)
{
    public Task Handle(DebitarTransferenciaExterna comando, CancellationToken cancellationToken) =>
        passo.ExecutarAsync(
            comando.TransferenciaId,
            comando.ContaId,
            comando.Moeda,
            conta => conta.DebitarTransferenciaExterna(
                comando.TransferenciaId,
                Dinheiro.De(comando.Valor, conta.Moeda),
                Dinheiro.De(comando.Tarifa, conta.Moeda),
                comando.IbanDestino,
                comando.NomeBeneficiario,
                comando.Descricao,
                relogio.GetUtcNow(),
                ConversaoDo(comando)),
            transferencia => respostas.DebitoRealizado(comando.TransferenciaId, comando.ContaId, transferencia.Valor, transferencia.Tarifa),
            motivo => respostas.DebitoRecusado(comando.TransferenciaId, comando.ContaId, motivo),
            cancellationToken);

    private static Conversao? ConversaoDo(DebitarTransferenciaExterna comando) =>
        comando is { MoedaDestino: { } moeda, ValorDestino: { } valor, Taxa: { } taxa }
            ? Conversao.Criar(Dinheiro.De(valor, Moedas.Ler(moeda)), taxa)
            : null;
}
