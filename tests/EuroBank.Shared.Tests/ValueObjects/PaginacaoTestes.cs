using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Shared.Tests.ValueObjects;

public sealed class PaginacaoTestes
{
    [Fact]
    public void Criar_SemParametros_UsaPaginaUmETamanhoCinquenta()
    {
        var paginacao = Paginacao.Criar(null, null);

        paginacao.Pagina.ShouldBe(1);
        paginacao.Tamanho.ShouldBe(50);
        paginacao.Saltar.ShouldBe(0);
    }

    [Fact]
    public void Saltar_CalculaOffsetDaPagina()
    {
        Paginacao.Criar(3, 20).Saltar.ShouldBe(40);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public void Criar_ForaDosLimites_LancaExcecaoPaginacaoInvalida(int pagina, int tamanho)
    {
        Should.Throw<ExcecaoPaginacaoInvalida>(() => Paginacao.Criar(pagina, tamanho));
    }

    [Fact]
    public void Criar_ComTamanhoMaximo_Aceita()
    {
        Paginacao.Criar(1, 200).Tamanho.ShouldBe(200);
    }
}
