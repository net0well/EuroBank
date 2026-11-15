using System.Globalization;
using EuroBank.Antifraude.Treinamento;
using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Enums;
using EuroBank.Antifraude.Treinamento.Generation;

// Treino do modelo de fraude do antifraude.
//   dotnet run --project src/Antifraude/EuroBank.Antifraude.Treinamento -- gerar [--semente 42] [--contas 2000] [--dias 90] [--saida dados/transferencias-sinteticas.csv]
var argumentos = Argumentos.Ler(args);

switch (argumentos.Comando)
{
    case "gerar":
        var opcoes = new OpcoesGeracao(
            argumentos.Inteiro("semente", 42),
            argumentos.Inteiro("contas", 2_000),
            argumentos.Inteiro("dias", 90));
        var saida = argumentos.Texto("saida", "dados/transferencias-sinteticas.csv");
        var linhas = new GeradorTransferencias(opcoes).Gerar();
        ArquivoTransferencias.Escrever(saida, linhas);

        var fraudes = linhas.Count(linha => linha.Fraude);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{linhas.Count} transferências em {opcoes.Dias} dias, {fraudes} fraudes ({100.0 * fraudes / linhas.Count:0.00}%): {saida}"));
        foreach (var padrao in Enum.GetValues<PadraoFraude>().Where(padrao => padrao != PadraoFraude.Nenhum))
        {
            Console.WriteLine($"  {padrao}: {linhas.Count(linha => linha.Padrao == padrao)}");
        }

        return 0;
    default:
        Console.Error.WriteLine("Comandos: gerar");
        return 1;
}
