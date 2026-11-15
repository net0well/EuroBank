using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Enums;
using EuroBank.Antifraude.Treinamento.Generation;
using EuroBank.Antifraude.Treinamento.TransferObjects;

namespace EuroBank.Antifraude.Treinamento.Tests.Generation;

/// <summary>O mundo sintético: reproduzível pela semente, com a taxa de fraude certa e a assinatura de cada padrão.</summary>
public sealed class GeradorTransferenciasTestes
{
    private static readonly OpcoesGeracao Pequeno = new(Semente: 7, Contas: 400, Dias: 60);

    private static readonly Lazy<IReadOnlyList<TransferenciaSintetica>> Linhas = new(() => new GeradorTransferencias(Pequeno).Gerar());

    private static IEnumerable<TransferenciaSintetica> Do(PadraoFraude padrao) => Linhas.Value.Where(linha => linha.Padrao == padrao);

    [Fact]
    public void MesmaSemente_MesmosDados_OutraSemente_OutrosDados()
    {
        var outraVez = new GeradorTransferencias(Pequeno).Gerar();
        var outraSemente = new GeradorTransferencias(Pequeno with { Semente = 8 }).Gerar();

        outraVez.ShouldBe(Linhas.Value);
        outraSemente.ShouldNotBe(Linhas.Value);
    }

    [Fact]
    public void TaxaDeFraude_PertoDe1Virgula5PorCento_ComOsQuatroPadroes()
    {
        var taxa = (double)Linhas.Value.Count(linha => linha.Fraude) / Linhas.Value.Count;

        taxa.ShouldBeInRange(0.008, 0.03);
        foreach (var padrao in Enum.GetValues<PadraoFraude>().Where(padrao => padrao != PadraoFraude.Nenhum))
        {
            Do(padrao).ShouldNotBeEmpty(padrao.ToString());
        }
    }

    [Fact]
    public void SoODentroDoPeriodo_EmOrdemDeTempo()
    {
        Linhas.Value.ShouldAllBe(linha => linha.Momento >= Pequeno.InicioEfetivo && linha.Momento < Pequeno.Fim);
        Linhas.Value.Select(linha => linha.Momento).ShouldBeInOrder();
    }

    [Fact]
    public void CadaPadrao_TemASuaAssinatura()
    {
        var normais = Do(PadraoFraude.Nenhum).ToList();

        Do(PadraoFraude.TomadaDeConta).Average(linha => linha.Features.HoraDoDia).ShouldBeLessThan(6);
        Do(PadraoFraude.TomadaDeConta).Average(linha => linha.Features.FracaoDoSaldo).ShouldBeGreaterThan(0.5f);
        Do(PadraoFraude.EngenhariaSocial).ShouldAllBe(linha => linha.Features.BeneficiarioNovo == 1);
        Do(PadraoFraude.EngenhariaSocial).Average(linha => linha.Features.ValorSobreMedia).ShouldBeGreaterThan(5);
        Do(PadraoFraude.TesteDeCartao).Average(linha => linha.Features.SaidasUltimaHora).ShouldBeGreaterThan(1);
        Do(PadraoFraude.MulaDeDinheiro).Average(linha => linha.Features.EntradasSobreValor).ShouldBeGreaterThan(1);
        Do(PadraoFraude.MulaDeDinheiro).Average(linha => linha.Features.IdadeContaDias).ShouldBeLessThan(30);
        normais.Average(linha => linha.Features.ValorSobreMedia).ShouldBeLessThan(2);
    }

    [Fact]
    public void Csv_IdaEVolta_SemPerder_Nada()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"eurobank-sintetico-{Guid.NewGuid():N}.csv");
        try
        {
            ArquivoTransferencias.Escrever(caminho, Linhas.Value);

            ArquivoTransferencias.Ler(caminho).ShouldBe(Linhas.Value);
        }
        finally
        {
            File.Delete(caminho);
        }
    }
}
