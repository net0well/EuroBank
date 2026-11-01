namespace EuroBank.Contas.Dominio.Contas;

/// <summary>Nome do titular da conta: obrigatório e com até 120 caracteres.</summary>
public sealed record Titular
{
    public const int TamanhoMaximo = 120;

    private Titular(string nome) => Nome = nome;

    public string Nome { get; }

    public static Titular Criar(string? nome)
    {
        var limpo = nome?.Trim() ?? string.Empty;
        if (limpo.Length == 0)
        {
            throw new ExcecaoTitularInvalido("o nome do titular é obrigatório");
        }

        if (limpo.Length > TamanhoMaximo)
        {
            throw new ExcecaoTitularInvalido($"o nome do titular deve ter até {TamanhoMaximo} caracteres");
        }

        return new Titular(limpo);
    }

    public override string ToString() => Nome;
}
