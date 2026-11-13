using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Interfaces;

/// <summary>
/// Projeção local das contas EuroBank, alimentada pelos eventos ContaAberta e ContaEncerrada de
/// eurobank.contas.eventos. É de onde Transferencias tira a moeda da conta de origem, acha a conta de destino de um
/// câmbio pelo IBAN e sabe se a conta de uma reconciliação ainda está aberta.
/// </summary>
public interface IContasConhecidas
{
    Task<ContaEuroBank?> ObterPorIdAsync(Guid contaId, CancellationToken cancellationToken);

    Task<ContaEuroBank?> ObterPorIbanAsync(string iban, CancellationToken cancellationToken);

    /// <summary>Registra a conta se ela ainda não está na projeção (eventos repetidos não mudam nada).</summary>
    Task<bool> RegistrarAsync(ContaEuroBank conta, DateTimeOffset abertaEm, CancellationToken cancellationToken);

    /// <summary>Marca a conta como encerrada (eventos repetidos não mudam nada).</summary>
    Task<bool> RegistrarEncerramentoAsync(Guid contaId, DateTimeOffset encerradaEm, CancellationToken cancellationToken);
}
