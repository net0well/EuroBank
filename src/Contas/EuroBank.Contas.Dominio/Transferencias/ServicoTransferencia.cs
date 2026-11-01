using EuroBank.Contas.Dominio.Comum;
using EuroBank.Contas.Dominio.Contas;

namespace EuroBank.Contas.Dominio.Transferencias;

/// <summary>
/// Serviço de domínio que coordena dois agregados Conta. Valida as duas pontas antes de alterar qualquer uma,
/// então uma regra violada nunca deixa uma conta alterada pela metade. A gravação das duas contas e dos dois
/// eventos acontece numa única transação, feita pela unidade de trabalho.
/// </summary>
public sealed class ServicoTransferencia
{
    /// <returns>O id que correlaciona os eventos TransferenciaEnviada e TransferenciaRecebida.</returns>
    public Guid Transferir(Conta origem, Conta destino, Dinheiro valor, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(origem);
        ArgumentNullException.ThrowIfNull(destino);

        if (origem.Id == destino.Id)
        {
            throw new ExcecaoTransferenciaParaMesmaConta();
        }

        origem.GarantirPodeEnviar(valor);
        destino.GarantirPodeReceber(valor);

        var transferenciaId = Guid.CreateVersion7();
        origem.EnviarTransferencia(transferenciaId, destino.Iban, valor, agora);
        destino.ReceberTransferencia(transferenciaId, origem.Iban, valor, agora);
        return transferenciaId;
    }
}

public sealed class ExcecaoTransferenciaParaMesmaConta()
    : ExcecaoRegraNegocio("transferencia_para_mesma_conta", "A conta de destino deve ser diferente da conta de origem.");
