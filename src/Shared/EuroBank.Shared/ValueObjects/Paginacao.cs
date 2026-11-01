using EuroBank.Shared.Exceptions;

namespace EuroBank.Shared.ValueObjects;

/// <summary>Paginação por offset: página a partir de 1 e tamanho entre 1 e 200 (padrão 50).</summary>
public sealed record Paginacao
{
    public const int TamanhoPadrao = 50;
    public const int TamanhoMaximo = 200;

    private Paginacao(int pagina, int tamanho)
    {
        Pagina = pagina;
        Tamanho = tamanho;
    }

    public int Pagina { get; }

    public int Tamanho { get; }

    /// <summary>Quantidade de itens a pular (OFFSET).</summary>
    public int Saltar => (Pagina - 1) * Tamanho;

    public static Paginacao Criar(int? pagina, int? tamanho)
    {
        var numero = pagina ?? 1;
        var itens = tamanho ?? TamanhoPadrao;

        if (numero < 1)
        {
            throw new ExcecaoPaginacaoInvalida("a página deve ser maior ou igual a 1");
        }

        if (itens is < 1 or > TamanhoMaximo)
        {
            throw new ExcecaoPaginacaoInvalida($"o tamanho deve estar entre 1 e {TamanhoMaximo}");
        }

        return new Paginacao(numero, itens);
    }
}
