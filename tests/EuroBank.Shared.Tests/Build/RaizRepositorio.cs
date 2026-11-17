namespace EuroBank.Shared.Tests.Build;

/// <summary>A pasta do EuroBank.slnx, subindo a partir da saída dos testes: os testes de build leem os arquivos dela.</summary>
public static class RaizRepositorio
{
    public static string Caminho { get; } = Achar();

    private static string Achar()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "EuroBank.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório (EuroBank.slnx) não encontrada.");
    }
}
