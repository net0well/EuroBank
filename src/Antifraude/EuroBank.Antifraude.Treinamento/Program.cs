using EuroBank.Antifraude.Treinamento;
using EuroBank.Antifraude.Treinamento.Commands;

// Treino do modelo de fraude do antifraude.
//   dotnet run --project src/Antifraude/EuroBank.Antifraude.Treinamento -- gerar [--semente 42] [--contas 2000] [--dias 90] [--saida dados/transferencias-sinteticas.csv]
//   dotnet run --project src/Antifraude/EuroBank.Antifraude.Treinamento -- treinar [--dados dados/transferencias-sinteticas.csv] [--semente 42]
//       [--auc-pr-minima 0.6] [--meta-recall 0.9] [--meta-sinalizadas 0.05]
//   O banco do Antifraude vem de ConnectionStrings__Antifraude (padrão: o Postgres do compose em localhost:5434).
var argumentos = Argumentos.Ler(args);

return argumentos.Comando switch
{
    "gerar" => ComandoGerar.Executar(argumentos),
    "treinar" => await ComandoTreinar.ExecutarAsync(argumentos, CancellationToken.None),
    _ => Ajuda(),
};

static int Ajuda()
{
    Console.Error.WriteLine("Comandos: gerar, treinar");
    return 1;
}
