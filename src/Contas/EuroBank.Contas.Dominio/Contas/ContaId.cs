namespace EuroBank.Contas.Dominio.Contas;

/// <summary>Identificador da conta (Guid v7). É também a chave da mensagem no Kafka.</summary>
public readonly record struct ContaId(Guid Valor)
{
    public static ContaId Nova() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
