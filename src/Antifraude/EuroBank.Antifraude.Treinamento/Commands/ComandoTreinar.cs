using System.Globalization;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Treinamento.Data;
using EuroBank.Antifraude.Treinamento.Generation;
using EuroBank.Antifraude.Treinamento.Training;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Treinamento.Commands;

/// <summary>
/// treinar: junta o CSV sintético (gerado na hora se faltar) e os rótulos reais do banco do Antifraude, treina, avalia
/// e registra a versão. Abaixo da AUC-PR mínima, sai com erro e não registra nada.
/// </summary>
public static class ComandoTreinar
{
    public const string ConexaoPadrao = "Host=localhost;Port=5434;Database=eurobank_antifraude;Username=eurobank;Password=eurobank";

    /// <summary>Código de saída quando o modelo não alcança a AUC-PR mínima.</summary>
    public const int AbaixoDoMinimo = 2;

    public static async Task<int> ExecutarAsync(Argumentos argumentos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(argumentos);
        var opcoes = new OpcoesTreino(
            Semente: argumentos.Inteiro("semente", 42),
            MetaRecall: argumentos.Real("meta-recall", 0.9),
            MetaTaxaSinalizada: argumentos.Real("meta-sinalizadas", 0.05),
            AucPrMinima: argumentos.Real("auc-pr-minima", 0.6));
        var caminho = argumentos.Texto("dados", ComandoGerar.ArquivoPadrao);
        if (!File.Exists(caminho))
        {
            Console.WriteLine($"{caminho} não existe: gerando com a semente {opcoes.Semente}.");
            ComandoGerar.Gerar(new OpcoesGeracao(opcoes.Semente), caminho);
        }

        var conjunto = ArquivoTransferencias.Ler(caminho);
        var sinteticas = conjunto.Transferencias
            .Select(transferencia => new LinhaTreino(transferencia.Momento, transferencia.Features, transferencia.Fraude, Real: false))
            .ToList();

        var conexao = Environment.GetEnvironmentVariable("ConnectionStrings__Antifraude") ?? ConexaoPadrao;
        await using var contexto = new AntifraudeDbContext(new DbContextOptionsBuilder<AntifraudeDbContext>().UseNpgsql(conexao).Options);
        var reais = await RotulosReais.CarregarAsync(contexto, cancellationToken);
        Console.WriteLine($"{sinteticas.Count} linhas sintéticas (semente {conjunto.Semente}) e {reais.Count} rótulos reais ({reais.Count(linha => linha.Fraude)} fraudes).");

        var resultado = new TreinadorModelo(opcoes).Treinar(conjunto.Semente, sinteticas, reais);
        Imprimir(resultado);
        if (resultado.Metricas.AucPr < opcoes.AucPrMinima)
        {
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"AUC-PR {resultado.Metricas.AucPr:0.000} abaixo do mínimo {opcoes.AucPrMinima:0.000}: a versão não foi registrada."));
            return AbaixoDoMinimo;
        }

        var versao = await RegistroModelos.RegistrarAsync(contexto, resultado, DateTimeOffset.UtcNow, cancellationToken);
        Console.WriteLine($"Versão {versao.Versao} registrada. Para usar: ative-a ou coloque-a em sombra na API do Antifraude.");
        return 0;
    }

    private static void Imprimir(ResultadoTreino resultado)
    {
        var metricas = resultado.Metricas;
        var dados = resultado.Dados;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"""
            Teste temporal: {dados.LinhasTeste} linhas de {dados.CorteTeste:yyyy-MM-dd} a {dados.FimTeste:yyyy-MM-dd}
            AUC-PR: {metricas.AucPr:0.000}
            Limiar recomendado: {resultado.LimiarRecomendado:0.00} (meta: recall >= {dados.MetaRecall:0.00} com no máximo {dados.MetaTaxaSinalizada:P0} sinalizadas; {(dados.MetaAtingida ? "atingida" : "não atingida")})
              precisão {metricas.Precisao:0.000}, recall {metricas.Recall:0.000}, F1 {metricas.F1:0.000}
              VP {metricas.Matriz.VerdadeirosPositivos}, FP {metricas.Matriz.FalsosPositivos}, VN {metricas.Matriz.VerdadeirosNegativos}, FN {metricas.Matriz.FalsosNegativos}
            Curva (limiar: precisão / recall / sinalizadas):
            """));
        foreach (var ponto in metricas.Curva)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {ponto.Limiar:0.00}: {ponto.Precisao:0.000} / {ponto.Recall:0.000} / {ponto.TaxaSinalizada:P1}"));
        }
    }
}
