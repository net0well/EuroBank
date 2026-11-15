using System.Globalization;
using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Enums;
using EuroBank.Antifraude.Treinamento.Generation;

namespace EuroBank.Antifraude.Treinamento.Commands;

/// <summary>gerar: simula o mundo e grava o CSV sintético.</summary>
public static class ComandoGerar
{
    public const string ArquivoPadrao = "dados/transferencias-sinteticas.csv";

    public static int Executar(Argumentos argumentos)
    {
        ArgumentNullException.ThrowIfNull(argumentos);
        Gerar(
            new OpcoesGeracao(argumentos.Inteiro("semente", 42), argumentos.Inteiro("contas", 2_000), argumentos.Inteiro("dias", 90)),
            argumentos.Texto("saida", ArquivoPadrao));
        return 0;
    }

    public static void Gerar(OpcoesGeracao opcoes, string saida)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        var linhas = new GeradorTransferencias(opcoes).Gerar();
        ArquivoTransferencias.Escrever(saida, opcoes.Semente, linhas);

        var fraudes = linhas.Count(linha => linha.Fraude);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{linhas.Count} transferências em {opcoes.Dias} dias, {fraudes} fraudes ({100.0 * fraudes / linhas.Count:0.00}%): {saida}"));
        foreach (var padrao in Enum.GetValues<PadraoFraude>().Where(padrao => padrao != PadraoFraude.Nenhum))
        {
            Console.WriteLine($"  {padrao}: {linhas.Count(linha => linha.Padrao == padrao)}");
        }
    }
}
