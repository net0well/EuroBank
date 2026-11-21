using System.Globalization;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Uma execução de treino, pedida pela web ou pela linha de comando: os parâmetros, as etapas com início e fim, o
/// progresso, o log, o relatório e a versão registrada. O treinador a conduz etapa a etapa e grava cada passo; quem
/// acompanha lê a execução. Só uma ativa por vez (garantido também por um índice único no banco).
/// </summary>
public sealed class ExecucaoTreino
{
    /// <summary>Linhas de log guardadas: as mais antigas saem primeiro (um treino inteiro gera bem menos que isso).</summary>
    public const int LimiteRegistros = 500;

    public const string SolicitanteLinhaDeComando = "linha de comando";

    /// <summary>O peso de cada etapa no progresso (soma 100): as árvores são a maior parte do tempo.</summary>
    private static readonly IReadOnlyDictionary<EtapaTreino, int> Pesos = new Dictionary<EtapaTreino, int>
    {
        [EtapaTreino.PreparandoDados] = 5,
        [EtapaTreino.CarregandoRotulos] = 5,
        [EtapaTreino.SeparandoTreinoETeste] = 5,
        [EtapaTreino.Treinando] = 55,
        [EtapaTreino.Avaliando] = 10,
        [EtapaTreino.Analisando] = 15,
        [EtapaTreino.Registrando] = 5,
    };

    private readonly List<EtapaExecutada> _etapas = [];
    private readonly List<RegistroTreino> _registros = [];

    private ExecucaoTreino()
    {
        // Usado pelo EF Core.
    }

    public Guid Id { get; private set; }

    public StatusTreino Status { get; private set; }

    public ParametrosTreino Parametros { get; private set; } = null!;

    public string Solicitante { get; private set; } = string.Empty;

    public IReadOnlyList<EtapaExecutada> Etapas => _etapas.AsReadOnly();

    /// <summary>A etapa que está rodando agora, se houver.</summary>
    public EtapaTreino? EtapaAtual => _etapas.LastOrDefault(etapa => etapa.Fim is null)?.Etapa;

    /// <summary>De 0 a 100, pelo peso das etapas concluídas e da fração da atual.</summary>
    public int Progresso { get; private set; }

    public IReadOnlyList<RegistroTreino> Registros => _registros.AsReadOnly();

    public RelatorioTreino? Relatorio { get; private set; }

    public int? Versao { get; private set; }

    public string? Erro { get; private set; }

    /// <summary>O cancelamento de uma execução em andamento, atendido quando a etapa atual terminar.</summary>
    public bool CancelamentoPedido { get; private set; }

    public DateTimeOffset SolicitadaEm { get; private set; }

    public DateTimeOffset? IniciadaEm { get; private set; }

    public DateTimeOffset? TerminadaEm { get; private set; }

    /// <summary>O último passo gravado: o sinal de vida do treinador.</summary>
    public DateTimeOffset AtualizadaEm { get; private set; }

    public bool Ativa => Status is StatusTreino.Solicitada or StatusTreino.EmAndamento;

    /// <exception cref="ExcecaoParametrosTreinoInvalidos">Algum parâmetro fora da faixa.</exception>
    /// <exception cref="ExcecaoAnalistaObrigatorio">Sem o nome de quem pediu.</exception>
    public static ExecucaoTreino Solicitar(ParametrosTreino parametros, string? solicitante, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        if (string.IsNullOrWhiteSpace(solicitante))
        {
            throw new ExcecaoAnalistaObrigatorio();
        }

        var execucao = new ExecucaoTreino
        {
            Id = Guid.CreateVersion7(agora),
            Status = StatusTreino.Solicitada,
            Parametros = parametros.Validar(),
            Solicitante = solicitante.Trim(),
            SolicitadaEm = agora,
            AtualizadaEm = agora,
        };
        execucao.Registrar($"Treino solicitado por {execucao.Solicitante}.", agora);
        return execucao;
    }

    public void Iniciar(DateTimeOffset agora)
    {
        Exigir(StatusTreino.Solicitada, "começar");
        Status = StatusTreino.EmAndamento;
        IniciadaEm = agora;
        Registrar("O treinador começou.", agora);
    }

    /// <summary>Começa a etapa, fechando a anterior se ela ainda estiver aberta.</summary>
    public void IniciarEtapa(EtapaTreino etapa, DateTimeOffset agora)
    {
        Exigir(StatusTreino.EmAndamento, "começar uma etapa");
        FecharEtapaAberta(agora, resumo: null);
        _etapas.Add(new EtapaExecutada(etapa, agora));
        Progresso = PesoConcluido();
        AtualizadaEm = agora;
    }

    /// <summary>A fração já feita da etapa atual (as árvores prontas, no treino).</summary>
    public void AvancarEtapa(double fracao, DateTimeOffset agora)
    {
        Exigir(StatusTreino.EmAndamento, "avançar");
        if (EtapaAtual is { } etapa)
        {
            Progresso = PesoConcluido() + (int)Math.Floor(Pesos[etapa] * Math.Clamp(fracao, 0, 1));
        }

        AtualizadaEm = agora;
    }

    public void ConcluirEtapa(string? resumo, DateTimeOffset agora)
    {
        Exigir(StatusTreino.EmAndamento, "concluir uma etapa");
        FecharEtapaAberta(agora, resumo);
        Progresso = PesoConcluido();
        if (resumo is not null)
        {
            Registrar(resumo, agora);
        }

        AtualizadaEm = agora;
    }

    public void Registrar(string mensagem, DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem);
        _registros.Add(new RegistroTreino(agora, mensagem));
        if (_registros.Count > LimiteRegistros)
        {
            _registros.RemoveRange(0, _registros.Count - LimiteRegistros);
        }

        AtualizadaEm = agora;
    }

    public void Concluir(RelatorioTreino relatorio, int versao, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(relatorio);
        Exigir(StatusTreino.EmAndamento, "concluir");
        FecharEtapaAberta(agora, resumo: null);
        Status = StatusTreino.Concluida;
        Relatorio = relatorio;
        Versao = versao;
        Progresso = 100;
        TerminadaEm = agora;
        Registrar($"Versão {versao} registrada: ative-a ou ponha-a em sombra nos modelos.", agora);
    }

    /// <summary>Avaliou, mas a AUC-PR não chegou ao mínimo: fica o relatório, sem versão.</summary>
    public void FicarAbaixoDoMinimo(RelatorioTreino relatorio, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(relatorio);
        Exigir(StatusTreino.EmAndamento, "terminar abaixo do mínimo");
        FecharEtapaAberta(agora, resumo: null);
        Status = StatusTreino.AbaixoDoMinimo;
        Relatorio = relatorio;
        Progresso = 100;
        TerminadaEm = agora;
        Erro = string.Create(
            CultureInfo.InvariantCulture,
            $"AUC-PR {relatorio.Metricas.AucPr:0.000} abaixo do mínimo {Parametros.AucPrMinima:0.000}: a versão não foi registrada.");
        Registrar(Erro, agora);
    }

    public void Falhar(string erro, DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(erro);
        if (!Ativa)
        {
            throw new ExcecaoTransicaoTreinoInvalida(Status, "falhar");
        }

        FecharEtapaAberta(agora, resumo: null);
        Status = StatusTreino.Falhou;
        Erro = erro;
        TerminadaEm = agora;
        Registrar($"Falhou: {erro}", agora);
    }

    /// <summary>
    /// Solicitada, termina na hora; em andamento, fica pedido e o treinador para quando a etapa atual terminar (as
    /// árvores do FastTree não se interrompem no meio).
    /// </summary>
    /// <exception cref="ExcecaoTreinoTerminado">A execução já terminou.</exception>
    public void PedirCancelamento(string? quem, DateTimeOffset agora)
    {
        var por = string.IsNullOrWhiteSpace(quem) ? string.Empty : $" por {quem.Trim()}";
        switch (Status)
        {
            case StatusTreino.Solicitada:
                Status = StatusTreino.Cancelada;
                TerminadaEm = agora;
                Registrar($"Cancelado{por} antes de começar.", agora);
                break;
            case StatusTreino.EmAndamento when !CancelamentoPedido:
                CancelamentoPedido = true;
                Registrar($"Cancelamento pedido{por}: vale quando a etapa atual terminar.", agora);
                break;
            case StatusTreino.EmAndamento:
                break;
            default:
                throw new ExcecaoTreinoTerminado(Id, Status);
        }
    }

    /// <summary>O treinador atendeu o cancelamento pedido.</summary>
    public void Cancelar(DateTimeOffset agora)
    {
        Exigir(StatusTreino.EmAndamento, "ser cancelado pelo treinador");
        FecharEtapaAberta(agora, "Interrompida pelo cancelamento.");
        Status = StatusTreino.Cancelada;
        TerminadaEm = agora;
        Registrar("Treino cancelado.", agora);
    }

    /// <summary>Em andamento e sem nenhum passo gravado há mais que o limite: o treinador parou no meio.</summary>
    public bool SemSinalDeVida(DateTimeOffset agora, TimeSpan limite) =>
        Status == StatusTreino.EmAndamento && agora - AtualizadaEm > limite;

    private void FecharEtapaAberta(DateTimeOffset agora, string? resumo)
    {
        var indice = _etapas.FindLastIndex(etapa => etapa.Fim is null);
        if (indice >= 0)
        {
            _etapas[indice] = _etapas[indice] with { Fim = agora, Resumo = resumo ?? _etapas[indice].Resumo };
        }
    }

    private int PesoConcluido() => _etapas.Where(etapa => etapa.Fim is not null).Sum(etapa => Pesos[etapa.Etapa]);

    private void Exigir(StatusTreino esperado, string operacao)
    {
        if (Status != esperado)
        {
            throw new ExcecaoTransicaoTreinoInvalida(Status, operacao);
        }
    }
}
