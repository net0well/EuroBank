using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Features.Services;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Worker.Tests.Fixtures;

namespace EuroBank.Antifraude.Worker.Tests.Perfil;

/// <summary>
/// Paridade entre o treino e a produção: os mesmos eventos de uma conta, aplicados no acumulador em memória (o
/// caminho do treino) e no perfil do Redis pelos manipuladores do worker (o caminho da produção), dão as mesmas
/// features em qualquer momento depois deles.
/// </summary>
public sealed class ParidadeFeaturesTestes(AntifraudeWorkerFixture worker)
{
    private static readonly DateTimeOffset Inicio = new(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);

    private static readonly string[] Ibans =
        ["DE89370400440532013000", "FR7630006000011234567890189", "ES9121000418450200051332", "PT50EURO0000000000002"];

    [Theory]
    [InlineData(11)]
    [InlineData(29)]
    [InlineData(73)]
    public async Task MesmosEventos_MesmasFeatures_NoTreinoENaProducao(int semente)
    {
        var aleatorio = new Random(semente);
        var conta = Guid.CreateVersion7();
        var acumulador = new AcumuladorHistorico();
        var momento = Inicio;
        var versao = 1;
        decimal saldo = 0;

        await AplicarAsync(new EventoContaAberta(conta, "PT50EURO0000000000001", "Ana", versao, momento, "EUR"), acumulador);
        for (var i = 0; i < 120; i++)
        {
            // Segundos inteiros: o Redis guarda o momento em milissegundos.
            momento = momento.AddSeconds(aleatorio.Next(60, 36 * 3600));
            versao++;
            switch (aleatorio.Next(10))
            {
                case < 4:
                    var entrada = Math.Round((decimal)(aleatorio.NextDouble() * 900) + 10, 2);
                    saldo += entrada;
                    await AplicarAsync(new EventoEntradaConta(conta, entrada, saldo, versao, momento), acumulador);
                    break;
                case < 8:
                    var saida = Math.Round((decimal)(aleatorio.NextDouble() * 300) + 1, 2);
                    saldo -= saida;
                    await AplicarAsync(
                        new EventoSaidaConta(conta, Guid.CreateVersion7(), Ibans[aleatorio.Next(Ibans.Length)], saida, saldo, versao, momento), acumulador);
                    break;
                default:
                    saldo -= 5m;
                    await AplicarAsync(new EventoMovimentoConta(conta, saldo, versao, momento), acumulador);
                    break;
            }

            if (i % 15 == 0)
            {
                // Um evento repetido (mesma versão): os dois caminhos o ignoram.
                await AplicarAsync(new EventoEntradaConta(conta, 99_999m, 99_999m, versao, momento), acumulador);
            }
        }

        foreach (var depois in new[] { TimeSpan.FromMinutes(1), TimeSpan.FromHours(3), TimeSpan.FromDays(2), TimeSpan.FromDays(20) })
        {
            var agora = momento + depois;
            var perfil = await worker.PerfilAsync(conta, agora);
            foreach (var operacao in new[]
            {
                new OperacaoAvaliada(agora, 50m, Ibans[0], PaisDeRisco: false),
                new OperacaoAvaliada(agora, 4_000m, "IT60X0542811101000000123456", PaisDeRisco: true),
                new OperacaoAvaliada(agora, 7m, Ibans[3], PaisDeRisco: false),
            })
            {
                var producao = CalculadoraFeatures.Calcular(perfil.ParaHistorico(), operacao);
                var treino = CalculadoraFeatures.Calcular(acumulador.Retrato(agora), operacao);
                producao.ShouldBe(treino, $"semente {semente}, {depois} depois do último evento, valor {operacao.Valor}");
            }
        }
    }

    private async Task AplicarAsync(object evento, AcumuladorHistorico acumulador)
    {
        await worker.EntregarAsync(evento);
        _ = evento switch
        {
            EventoContaAberta aberta => acumulador.Abrir(aberta.Versao, aberta.OcorridoEm),
            EventoEntradaConta entrada => acumulador.Entrada(entrada.Versao, entrada.OcorridoEm, entrada.Valor, entrada.SaldoApos),
            EventoSaidaConta saida => acumulador.Saida(saida.Versao, saida.OcorridoEm, saida.Valor, saida.IbanDestino, saida.SaldoApos),
            EventoMovimentoConta movimento => acumulador.Movimento(movimento.Versao, movimento.SaldoApos),
            _ => throw new InvalidOperationException($"Evento sem caminho no acumulador: {evento.GetType().Name}"),
        };
    }
}
