using EuroBank.Contas.Domain.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Entities;

/// <summary>Registro, dentro do agregado Conta, de uma transferência pedida por saga e já debitada.</summary>
public sealed class TransferenciaDaConta
{
    private TransferenciaDaConta()
    {
        // Usado pelo EF Core.
    }

    internal TransferenciaDaConta(Guid id, Dinheiro valor, Dinheiro tarifa, string nomeBeneficiario, DateTimeOffset debitadaEm)
    {
        Id = id;
        Valor = valor;
        Tarifa = tarifa;
        NomeBeneficiario = nomeBeneficiario;
        Status = StatusTransferenciaDaConta.Pendente;
        DebitadaEm = debitadaEm;
    }

    /// <summary>Id da transferência na saga (o mesmo id que o orquestrador usa).</summary>
    public Guid Id { get; private set; }

    public Dinheiro Valor { get; private set; } = Dinheiro.Zero;

    public Dinheiro Tarifa { get; private set; } = Dinheiro.Zero;

    public string NomeBeneficiario { get; private set; } = string.Empty;

    public StatusTransferenciaDaConta Status { get; private set; }

    public DateTimeOffset DebitadaEm { get; private set; }

    public DateTimeOffset? EstornadaEm { get; private set; }

    internal void Estornar(DateTimeOffset agora)
    {
        Status = StatusTransferenciaDaConta.Estornada;
        EstornadaEm = agora;
    }

    internal void Liberar() => Status = StatusTransferenciaDaConta.Liberada;
}
