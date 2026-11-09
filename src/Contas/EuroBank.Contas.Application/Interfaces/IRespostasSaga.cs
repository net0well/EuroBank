using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Interfaces;

/// <summary>
/// Respostas de Contas ao orquestrador (eurobank.contas.respostas). São gravadas na outbox na mesma transação da
/// alteração da conta, inclusive quando a resposta é uma recusa e nada mudou.
/// </summary>
public interface IRespostasSaga
{
    void DebitoRealizado(Guid transferenciaId, Guid contaId, Dinheiro valor, Dinheiro tarifa);

    void DebitoRecusado(Guid transferenciaId, Guid contaId, string motivo);

    void EstornoRealizado(Guid transferenciaId, Guid contaId, Dinheiro valorDevolvido);
}
