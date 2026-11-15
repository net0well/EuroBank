using System.Globalization;
using System.Text;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Treinamento.Enums;
using EuroBank.Antifraude.Treinamento.TransferObjects;

namespace EuroBank.Antifraude.Treinamento.Data;

/// <summary>
/// O CSV do conjunto sintético: momento, conta, valor, padrão, rótulo e as features com os nomes do vetor. Cultura
/// invariante (ponto decimal), para o arquivo ser o mesmo em qualquer máquina.
/// </summary>
public static class ArquivoTransferencias
{
    private static readonly string Cabecalho =
        string.Join(',', ["Momento", "ContaId", "Valor", "Padrao", "Fraude", .. VetorFeatures.Nomes]);

    public static void Escrever(string caminho, IEnumerable<TransferenciaSintetica> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        var pasta = Path.GetDirectoryName(Path.GetFullPath(caminho));
        if (!string.IsNullOrEmpty(pasta))
        {
            Directory.CreateDirectory(pasta);
        }

        using var escritor = new StreamWriter(caminho, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        escritor.WriteLine(Cabecalho);
        foreach (var linha in linhas)
        {
            var colunas = new List<string>
            {
                linha.Momento.ToString("O", CultureInfo.InvariantCulture),
                linha.ContaId.ToString(),
                linha.Valor.ToString(CultureInfo.InvariantCulture),
                linha.Padrao.ToString(),
                linha.Fraude ? "1" : "0",
            };
            colunas.AddRange(linha.Features.ParaArray().Select(valor => valor.ToString("R", CultureInfo.InvariantCulture)));
            escritor.WriteLine(string.Join(',', colunas));
        }
    }

    public static IReadOnlyList<TransferenciaSintetica> Ler(string caminho)
    {
        var linhas = new List<TransferenciaSintetica>();
        using var leitor = new StreamReader(caminho);
        var cabecalho = leitor.ReadLine();
        if (cabecalho != Cabecalho)
        {
            throw new InvalidDataException($"Cabeçalho inesperado em {caminho}: gere o arquivo de novo com esta versão.");
        }

        while (leitor.ReadLine() is { } texto)
        {
            var colunas = texto.Split(',');
            linhas.Add(new TransferenciaSintetica(
                DateTimeOffset.Parse(colunas[0], CultureInfo.InvariantCulture),
                Guid.Parse(colunas[1]),
                decimal.Parse(colunas[2], CultureInfo.InvariantCulture),
                Enum.Parse<PadraoFraude>(colunas[3]),
                VetorFeatures.DeArray(colunas.Skip(5).Select(valor => float.Parse(valor, CultureInfo.InvariantCulture)).ToList())));
        }

        return linhas;
    }
}
