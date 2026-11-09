namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>Identificador da transferência (Guid v7). Vai em todos os comandos e respostas da saga.</summary>
public readonly record struct TransferenciaId(Guid Valor)
{
    public static TransferenciaId Nova() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
