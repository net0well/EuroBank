using EuroBank.Contas.Domain.Enums;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Entities;

/// <summary>
/// Registro, dentro do agregado Conta, de uma transferência pedida por saga: debitada (trava a conta até o fim da
/// saga) ou recebida (crédito de câmbio). Também é o registro de idempotência pelo id da transferência.
/// </summary>
public sealed class TransferenciaDaConta
{
    private decimal _valor;
    private decimal _tarifa;

    private TransferenciaDaConta()
    {
        // Usado pelo EF Core.
    }

    internal TransferenciaDaConta(Guid id, Dinheiro valor, Dinheiro tarifa, string nomeBeneficiario, DateTimeOffset debitadaEm)
    {
        Id = id;
        Moeda = valor.Moeda;
        Valor = valor;
        Tarifa = tarifa;
        NomeBeneficiario = nomeBeneficiario;
        Status = StatusTransferenciaDaConta.Pendente;
        DebitadaEm = debitadaEm;
    }

    /// <summary>Crédito recebido de uma saga de câmbio: não trava a conta, só evita creditar duas vezes.</summary>
    internal static TransferenciaDaConta Recebida(Guid id, Dinheiro valor, string nomeOrigem, DateTimeOffset recebidaEm) =>
        new(id, valor, Dinheiro.ZeroEm(valor.Moeda), nomeOrigem, recebidaEm) { Status = StatusTransferenciaDaConta.Recebida };

    /// <summary>Id da transferência na saga (o mesmo id que o orquestrador usa).</summary>
    public Guid Id { get; private set; }

    /// <summary>A moeda da conta; valor e tarifa são persistidos só como números.</summary>
    public Moeda Moeda { get; private set; }

    public Dinheiro Valor
    {
        get => Dinheiro.De(_valor, Moeda);
        private set => _valor = value.Valor;
    }

    public Dinheiro Tarifa
    {
        get => Dinheiro.De(_tarifa, Moeda);
        private set => _tarifa = value.Valor;
    }

    public string NomeBeneficiario { get; private set; } = string.Empty;

    public StatusTransferenciaDaConta Status { get; private set; }

    /// <summary>Quando foi debitada (ou creditada, num recebimento).</summary>
    public DateTimeOffset DebitadaEm { get; private set; }

    public DateTimeOffset? EstornadaEm { get; private set; }

    internal void Estornar(DateTimeOffset agora)
    {
        Status = StatusTransferenciaDaConta.Estornada;
        EstornadaEm = agora;
    }

    internal void Liberar() => Status = StatusTransferenciaDaConta.Liberada;
}
