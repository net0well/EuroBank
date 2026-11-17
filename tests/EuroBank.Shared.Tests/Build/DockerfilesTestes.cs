using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EuroBank.Shared.Tests.Build;

/// <summary>
/// Cada Dockerfile restaura antes de copiar o código, só com os .csproj: se faltar o de uma referência, a imagem não
/// constrói, e nenhum outro teste percebe (eles não constroem imagens). Aqui, cada Dockerfile tem de copiar o .csproj
/// do projeto que publica e o de todas as referências dele, diretas e indiretas.
/// </summary>
public sealed partial class DockerfilesTestes
{
    private static readonly string Raiz = AcharRaiz();

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

    private static string AcharRaiz()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "EuroBank.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório (EuroBank.slnx) não encontrada.");
    }

    [GeneratedRegex(@"dotnet publish (\S+\.csproj)")]
    private static partial Regex Publicacao();

    [GeneratedRegex(@"^COPY (\S+\.csproj) ", RegexOptions.Multiline)]
    private static partial Regex CopiaCsproj();
}
