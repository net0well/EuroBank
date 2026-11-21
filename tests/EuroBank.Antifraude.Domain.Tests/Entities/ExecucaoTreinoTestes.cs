using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

public sealed class ExecucaoTreinoTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    private static ExecucaoTreino Solicitada(ParametrosTreino? parametros = null) =>
        ExecucaoTreino.Solicitar(parametros ?? new ParametrosTreino(), " Marta ", Agora);

    private static ExecucaoTreino EmAndamento()
    {
        var execucao = Solicitada();
        execucao.Iniciar(Agora.AddSeconds(1));
        return execucao;
    }

    private static RelatorioTreino Relatorio(double aucPr = 0.95) => new(
        new DadosTreino(42, 10_000, 150, 3, 1, Agora.AddDays(-90), Agora.AddDays(-20), Agora, 2_000, 0.9, 0.05, MetaAtingida: true),
        new MetricasModelo(aucPr, 0.5, 0.9, 0.64, new MatrizConfusao(90, 90, 9_800, 10), []),
        0.5,
        [new PontoAprendizado(10, 0.8, 0.7)],
        [new ImportanciaFeature("LogValor", 1, 0.2)],
        [new FaixaScore(0, 0.05, 1, 900)],
        [new FaixaCalibracao(0, 0.1, 900, 0.01, 0.002)],
        [new RecallPadrao("TomadaDeConta", 10, 9, 0.9)],
        Ativa: null);

    [Fact]
    public void Solicitar_NasceSolicitada_ComOSolicitanteSemEspacos()
    {
        var execucao = Solicitada();

        execucao.Status.ShouldBe(StatusTreino.Solicitada);
        execucao.Ativa.ShouldBeTrue();
        execucao.Solicitante.ShouldBe("Marta");
        execucao.Progresso.ShouldBe(0);
        execucao.Registros.ShouldHaveSingleItem().Mensagem.ShouldContain("Marta");
    }

    [Fact]
    public void Solicitar_SemSolicitante_Recusa()
    {
        Should.Throw<ExcecaoAnalistaObrigatorio>(() => ExecucaoTreino.Solicitar(new ParametrosTreino(), "  ", Agora));
    }

    [Fact]
    public void Solicitar_ComParametroForaDaFaixa_Recusa()
    {
        var erro = Should.Throw<ExcecaoParametrosTreinoInvalidos>(() => Solicitada(new ParametrosTreino(Arvores: 5)));

        erro.Message.ShouldContain("arvores");
    }

    [Fact]
    public void Etapas_FechamUmaAntesDaOutra_EOProgressoSegueOsPesos()
    {
        var execucao = EmAndamento();

        execucao.IniciarEtapa(EtapaTreino.PreparandoDados, Agora.AddSeconds(2));
        execucao.ConcluirEtapa("99.167 linhas sintéticas.", Agora.AddSeconds(3));
        execucao.IniciarEtapa(EtapaTreino.CarregandoRotulos, Agora.AddSeconds(3));
        execucao.IniciarEtapa(EtapaTreino.SeparandoTreinoETeste, Agora.AddSeconds(4));
        execucao.IniciarEtapa(EtapaTreino.Treinando, Agora.AddSeconds(5));
        execucao.AvancarEtapa(0.5, Agora.AddSeconds(6));

        execucao.EtapaAtual.ShouldBe(EtapaTreino.Treinando);
        execucao.Progresso.ShouldBe(15 + 27);
        execucao.Etapas.Count(etapa => etapa.Fim is not null).ShouldBe(3);
        execucao.Etapas[0].Resumo.ShouldBe("99.167 linhas sintéticas.");
        execucao.AtualizadaEm.ShouldBe(Agora.AddSeconds(6));
    }

    [Fact]
    public void Concluir_GuardaRelatorioEVersao_EFechaAEtapa()
    {
        var execucao = EmAndamento();
        execucao.IniciarEtapa(EtapaTreino.Registrando, Agora.AddSeconds(2));

        execucao.Concluir(Relatorio(), 7, Agora.AddSeconds(3));

        execucao.Status.ShouldBe(StatusTreino.Concluida);
        execucao.Ativa.ShouldBeFalse();
        execucao.Versao.ShouldBe(7);
        execucao.Progresso.ShouldBe(100);
        execucao.TerminadaEm.ShouldBe(Agora.AddSeconds(3));
        execucao.EtapaAtual.ShouldBeNull();
        execucao.Relatorio.ShouldNotBeNull().RecallPorPadrao.ShouldHaveSingleItem().Recall.ShouldBe(0.9);
    }

    [Fact]
    public void AbaixoDoMinimo_FicaComORelatorioSemVersao()
    {
        var execucao = EmAndamento();

        execucao.FicarAbaixoDoMinimo(Relatorio(aucPr: 0.4), Agora.AddSeconds(2));

        execucao.Status.ShouldBe(StatusTreino.AbaixoDoMinimo);
        execucao.Versao.ShouldBeNull();
        execucao.Relatorio.ShouldNotBeNull();
        execucao.Erro.ShouldBe("AUC-PR 0.400 abaixo do mínimo 0.600: a versão não foi registrada.");
    }

    [Fact]
    public void CancelarSolicitada_TerminaNaHora()
    {
        var execucao = Solicitada();

        execucao.PedirCancelamento("Marta", Agora.AddSeconds(1));

        execucao.Status.ShouldBe(StatusTreino.Cancelada);
        execucao.TerminadaEm.ShouldBe(Agora.AddSeconds(1));
    }

    [Fact]
    public void CancelarEmAndamento_FicaPedido_EOTreinadorAtendeNaFronteira()
    {
        var execucao = EmAndamento();
        execucao.IniciarEtapa(EtapaTreino.Treinando, Agora.AddSeconds(2));

        execucao.PedirCancelamento("Marta", Agora.AddSeconds(3));
        execucao.PedirCancelamento("Marta", Agora.AddSeconds(4));

        execucao.Status.ShouldBe(StatusTreino.EmAndamento);
        execucao.CancelamentoPedido.ShouldBeTrue();
        execucao.Registros.Count(registro => registro.Mensagem.StartsWith("Cancelamento pedido", StringComparison.Ordinal)).ShouldBe(1);

        execucao.Cancelar(Agora.AddSeconds(5));

        execucao.Status.ShouldBe(StatusTreino.Cancelada);
        execucao.Etapas.ShouldHaveSingleItem().Resumo.ShouldBe("Interrompida pelo cancelamento.");
    }

    [Fact]
    public void CancelarTerminada_Recusa()
    {
        var execucao = EmAndamento();
        execucao.Falhar("disco cheio", Agora.AddSeconds(2));

        Should.Throw<ExcecaoTreinoTerminado>(() => execucao.PedirCancelamento("Marta", Agora.AddSeconds(3)));
        execucao.Erro.ShouldBe("disco cheio");
    }

    [Fact]
    public void IniciarDuasVezes_Recusa()
    {
        var execucao = EmAndamento();

        Should.Throw<ExcecaoTransicaoTreinoInvalida>(() => execucao.Iniciar(Agora.AddSeconds(2)));
    }

    [Fact]
    public void SemSinalDeVida_SoEmAndamentoEDepoisDoLimite()
    {
        var execucao = EmAndamento();

        execucao.SemSinalDeVida(Agora.AddMinutes(1), TimeSpan.FromMinutes(2)).ShouldBeFalse();
        execucao.SemSinalDeVida(Agora.AddMinutes(3), TimeSpan.FromMinutes(2)).ShouldBeTrue();
        Solicitada().SemSinalDeVida(Agora.AddHours(1), TimeSpan.FromMinutes(2)).ShouldBeFalse();
    }

    [Fact]
    public void Registros_GuardamSoOsMaisRecentes()
    {
        var execucao = EmAndamento();

        for (var linha = 0; linha < ExecucaoTreino.LimiteRegistros + 20; linha++)
        {
            execucao.Registrar($"linha {linha}", Agora.AddSeconds(2));
        }

        execucao.Registros.Count.ShouldBe(ExecucaoTreino.LimiteRegistros);
        execucao.Registros[^1].Mensagem.ShouldBe($"linha {ExecucaoTreino.LimiteRegistros + 19}");
    }
}
