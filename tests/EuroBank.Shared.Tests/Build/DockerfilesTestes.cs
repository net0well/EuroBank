using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EuroBank.Shared.Tests.Build;

/// <summary>
/// Cada Dockerfile restaura antes de copiar o código, só com os .csproj: se faltar o de uma referência, a imagem não
/// constrói, e nenhum outro teste percebe (eles não constroem imagens). Aqui, cada Dockerfile tem de copiar o .csproj
/// do projeto que publica e o de todas as referências dele, diretas e indiretas, e os .proto que elas ligam de fora da
/// pasta (o contrato do antifraude, que os clientes geram do mesmo arquivo). E o global.json vai junto: sem ele, uma
/// imagem de SDK antiga em cache publica sem reclamar, e sem os assets que as versões novas trazem.
/// </summary>
public sealed partial class DockerfilesTestes
{
    private static readonly string Raiz = RaizRepositorio.Caminho;

    public static TheoryData<string> Dockerfiles =>
        [.. Directory.GetFiles(Path.Combine(Raiz, "src"), "Dockerfile", SearchOption.AllDirectories).Select(Relativo).Order()];

    [Theory]
    [MemberData(nameof(Dockerfiles))]
    public void Dockerfile_CopiaOCsprojDeTodasAsReferencias(string dockerfile)
    {
        var texto = File.ReadAllText(Path.Combine(Raiz, dockerfile));
        var publicado = Publicacao().Match(texto).Groups[1].Value;
        var copiados = CopiaCsproj().Matches(texto).Select(copia => copia.Groups[1].Value).ToHashSet();

        publicado.ShouldNotBeNullOrEmpty();
        Referencias(publicado).Except(copiados).ShouldBeEmpty($"{dockerfile} não copia estes .csproj antes do restore");
    }

    [Theory]
    [MemberData(nameof(Dockerfiles))]
    public void Dockerfile_CopiaOsProtoLigadosPelasReferencias(string dockerfile)
    {
        var texto = File.ReadAllText(Path.Combine(Raiz, dockerfile));
        var origens = Copia().Matches(texto).Select(copia => copia.Groups[1].Value).ToList();
        var protos = Referencias(Publicacao().Match(texto).Groups[1].Value).SelectMany(Protos).ToHashSet();

        protos.Where(proto => !origens.Any(origem => origem == proto || (origem.EndsWith('/') && proto.StartsWith(origem, StringComparison.Ordinal))))
            .ShouldBeEmpty($"{dockerfile} não copia estes .proto");
    }

    [Theory]
    [MemberData(nameof(Dockerfiles))]
    public void Dockerfile_CopiaOGlobalJsonAntesDoRestore(string dockerfile)
    {
        var linhas = File.ReadAllLines(Path.Combine(Raiz, dockerfile));
        var copia = Array.FindIndex(linhas, linha => linha.StartsWith("COPY ", StringComparison.Ordinal) && linha.Contains(" global.json ", StringComparison.Ordinal));
        var restore = Array.FindIndex(linhas, linha => linha.Contains("dotnet restore", StringComparison.Ordinal));

        copia.ShouldBeGreaterThanOrEqualTo(0, $"{dockerfile} não copia o global.json");
        copia.ShouldBeLessThan(restore, $"{dockerfile} copia o global.json depois do restore");
    }

    /// <summary>Os arquivos dos itens Protobuf de um projeto, relativos à raiz.</summary>
    private static IEnumerable<string> Protos(string projeto)
    {
        var pasta = Path.GetDirectoryName(Path.Combine(Raiz, projeto))!;
        return XDocument.Load(Path.Combine(Raiz, projeto)).Descendants("Protobuf")
            .Select(item => Relativo(Path.GetFullPath(Path.Combine(pasta, item.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)))));
    }

    /// <summary>O projeto e todas as referências dele, com caminhos relativos à raiz e barras normais.</summary>
    private static HashSet<string> Referencias(string projeto)
    {
        var encontrados = new HashSet<string>();
        var pendentes = new Stack<string>([projeto]);
        while (pendentes.TryPop(out var atual))
        {
            if (!encontrados.Add(atual))
            {
                continue;
            }

            var pasta = Path.GetDirectoryName(Path.Combine(Raiz, atual))!;
            foreach (var referencia in XDocument.Load(Path.Combine(Raiz, atual)).Descendants("ProjectReference"))
            {
                var incluido = referencia.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar);
                pendentes.Push(Relativo(Path.GetFullPath(Path.Combine(pasta, incluido))));
            }
        }

        return encontrados;
    }

    private static string Relativo(string caminho) => Path.GetRelativePath(Raiz, caminho).Replace('\\', '/');

    [GeneratedRegex(@"dotnet publish (\S+\.csproj)")]
    private static partial Regex Publicacao();

    [GeneratedRegex(@"^COPY (\S+\.csproj) ", RegexOptions.Multiline)]
    private static partial Regex CopiaCsproj();

    [GeneratedRegex(@"^COPY (?!--)(\S+) ", RegexOptions.Multiline)]
    private static partial Regex Copia();
}
