namespace EuroBank.Shared.Persistence.Outbox;

/// <summary>Configuração da seção "Outbox" do appsettings.</summary>
public sealed class OpcoesOutbox
{
    public const string Secao = "Outbox";

    /// <summary>
    /// false (padrão): as linhas ficam na tabela para inspeção. true: cada linha é apagada na mesma transação
    /// em que foi inserida; o Debezium ainda publica, porque o INSERT continua no WAL.
    /// </summary>
    public bool ApagarAposGravar { get; set; }
}
