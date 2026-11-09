using System.Diagnostics;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.Policies;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Entities;

/// <summary>
/// Orquestrador da saga de transferência externa, como agregado. Guarda em que passo a saga está, decide o
/// próximo comando a cada resposta ou prazo vencido e registra a decisão como evento (que a outbox transforma no
/// comando para Contas ou para o banco externo). Respostas que não cabem no estado atual são ignoradas.
/// </summary>
/// <remarks>
/// Externa: Agendada, AguardandoDebito, Debitada, AguardandoLiquidacao, [ConsultandoLiquidacao], Concluida.
/// Falha depois do débito: AguardandoEstorno e Cancelada. Falha antes do débito: Recusada.
/// </remarks>
public sealed class Transferencia : RaizDeAgregado<TransferenciaId>
{
    private readonly List<PassoTransferencia> _historico = [];

    private Transferencia()
    {
        // Usado pelo EF Core.
    }

    public TipoTransferencia Tipo { get; private set; }

    /// <summary>Id da conta no contexto Contas (outro bounded context, por isso só o Guid).</summary>
    public Guid ContaOrigemId { get; private set; }

    public IbanDestino IbanDestino { get; private set; } = null!;

    public string Bic { get; private set; } = string.Empty;

    public string NomeBeneficiario { get; private set; } = string.Empty;

    public Dinheiro Valor { get; private set; } = Dinheiro.Zero;

    public Dinheiro Tarifa { get; private set; } = Dinheiro.Zero;

    public string? Descricao { get; private set; }

    public string? EmailNotificacao { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Impressão digital do corpo do pedido: a mesma Idempotency-Key com outro corpo é recusada.</summary>
    public string HashDoPedido { get; private set; } = string.Empty;

    public DateTimeOffset SolicitadaEm { get; private set; }

    /// <summary>Data pedida pelo cliente (nula quando é para já).</summary>
    public DateTimeOffset? ExecutarEm { get; private set; }

    /// <summary>Quando a saga realmente começa.</summary>
    public DateTimeOffset ExecutarEmEfetivo { get; private set; }

    public EstadoTransferencia Estado { get; private set; }

    public MotivoTransferencia? Motivo { get; private set; }

    /// <summary>O cliente pediu o cancelamento enquanto o débito estava em andamento.</summary>
    public bool CancelamentoSolicitado { get; private set; }

    /// <summary>
    /// A saga desistiu de esperar o débito, mas o comando continua no Kafka e Contas ainda pode debitar. Se o
    /// DebitoRealizado chegar depois, a saga estorna.
    /// </summary>
    public bool DebitoSemResposta { get; private set; }

    /// <summary>Envios do comando do passo atual (1 é o envio original).</summary>
    public int TentativasNoPasso { get; private set; }

    /// <summary>Até quando a saga espera a resposta do passo atual; nulo quando não espera nada.</summary>
    public DateTimeOffset? PrazoDoPasso { get; private set; }

    /// <summary>Cresce a cada passo do histórico; também é o token de concorrência otimista.</summary>
    public int Versao { get; private set; }

    public DateTimeOffset AtualizadaEm { get; private set; }

    public IReadOnlyList<PassoTransferencia> Historico => _historico.AsReadOnly();

    public bool EstaNaHora(DateTimeOffset agora) => Estado == EstadoTransferencia.Agendada && ExecutarEmEfetivo <= agora;

    public bool MesmoPedido(string hashDoPedido) => string.Equals(HashDoPedido, hashDoPedido, StringComparison.Ordinal);

    public static Transferencia SolicitarExterna(
        Guid contaOrigemId,
        IbanDestino ibanDestino,
        string? bic,
        string? nomeBeneficiario,
        Dinheiro valor,
        string? descricao,
        string? emailNotificacao,
        string idempotencyKey,
        string hashDoPedido,
        DateTimeOffset? executarEm,
        DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDoPedido);
        if (ibanDestino.EhEuroBank)
        {
            throw new ExcecaoDestinoEuroBank();
        }

        if (string.IsNullOrWhiteSpace(bic))
        {
            throw new ExcecaoBicObrigatorio();
        }

        if (string.IsNullOrWhiteSpace(nomeBeneficiario))
        {
            throw new ExcecaoBeneficiarioObrigatorio();
        }

        if (valor.EhZero)
        {
            throw new ExcecaoValorInvalido("o valor da transferência deve ser maior que zero");
        }

        var transferencia = new Transferencia
        {
            Id = TransferenciaId.Nova(),
            Tipo = TipoTransferencia.Externa,
            ContaOrigemId = contaOrigemId,
            IbanDestino = ibanDestino,
            Bic = bic.Trim().ToUpperInvariant(),
            NomeBeneficiario = nomeBeneficiario.Trim(),
            Valor = valor,
            Tarifa = TabelaTransferencias.TarifaExterna,
            Descricao = descricao,
            EmailNotificacao = emailNotificacao,
            IdempotencyKey = idempotencyKey,
            HashDoPedido = hashDoPedido,
            SolicitadaEm = agora,
            ExecutarEm = executarEm,
            ExecutarEmEfetivo = executarEm > agora ? executarEm.Value : agora,
            Estado = EstadoTransferencia.Agendada,
        };

        transferencia.AnotarPasso("Transferência solicitada", agora);
        return transferencia;
    }

    /// <summary>
    /// Começa a saga na hora marcada: confere o limite diário (só o valor, sem a tarifa) e pede o débito.
    /// Ignorado fora da hora ou se já começou (a API e a varredura podem tentar ao mesmo tempo).
    /// </summary>
    public ResultadoPasso Iniciar(Dinheiro jaTransferidoNoDia, PoliticaSaga politica, DateTimeOffset agora)
    {
        if (!EstaNaHora(agora))
        {
            return ResultadoPasso.Ignorado;
        }

        if (jaTransferidoNoDia + Valor > TabelaTransferencias.LimiteDiario)
        {
            Recusar(MotivoTransferencia.LimiteDiarioExcedido, $"Limite diário de {TabelaTransferencias.LimiteDiario} EUR excedido", agora);
            return ResultadoPasso.Aplicado;
        }

        EntrarNoPasso(EstadoTransferencia.AguardandoDebito, politica, agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarDebitoRealizado(PoliticaSaga politica, DateTimeOffset agora)
    {
        if (Estado == EstadoTransferencia.AguardandoDebito)
        {
            MudarPara(EstadoTransferencia.Debitada, "DebitoRealizado recebido de Contas", agora);
            if (CancelamentoSolicitado)
            {
                PedirEstorno(MotivoTransferencia.CanceladaPeloCliente, politica, agora);
            }
            else
            {
                EntrarNoPasso(EstadoTransferencia.AguardandoLiquidacao, politica, agora);
            }

            return ResultadoPasso.Aplicado;
        }

        if (DebitoSemResposta && Estado is EstadoTransferencia.Recusada or EstadoTransferencia.Cancelada)
        {
            DebitoSemResposta = false;
            AnotarPasso("DebitoRealizado chegou depois de a saga desistir: o débito precisa ser estornado", agora);
            PedirEstorno(Motivo!.Value, politica, agora);
            return ResultadoPasso.Aplicado;
        }

        return Ignorar("DebitoRealizado", agora);
    }

    public ResultadoPasso RegistrarDebitoRecusado(MotivoTransferencia motivo, DateTimeOffset agora)
    {
        if (Estado == EstadoTransferencia.AguardandoDebito)
        {
            if (CancelamentoSolicitado)
            {
                Cancelar(MotivoTransferencia.CanceladaPeloCliente, $"DebitoRecusado ({motivo}) recebido; nada a estornar", agora);
            }
            else
            {
                Recusar(motivo, $"DebitoRecusado recebido de Contas: {motivo}", agora);
            }

            return ResultadoPasso.Aplicado;
        }

        if (DebitoSemResposta)
        {
            DebitoSemResposta = false;
            AnotarPasso($"DebitoRecusado ({motivo}) chegou depois de a saga desistir: nada foi debitado", agora);
            return ResultadoPasso.Aplicado;
        }

        return Ignorar("DebitoRecusado", agora);
    }

    public ResultadoPasso RegistrarLiquidacaoConfirmada(DateTimeOffset agora)
    {
        if (!AguardaOBanco)
        {
            return Ignorar("LiquidacaoConfirmada", agora);
        }

        Concluir("LiquidacaoConfirmada recebida do banco externo", agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarLiquidacaoRecusada(PoliticaSaga politica, DateTimeOffset agora)
    {
        if (!AguardaOBanco)
        {
            return Ignorar("LiquidacaoRecusada", agora);
        }

        AnotarPasso("LiquidacaoRecusada recebida do banco externo", agora);
        PedirEstorno(MotivoTransferencia.RecusadaPeloBanco, politica, agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarStatusLiquidacao(StatusLiquidacaoExterna status, PoliticaSaga politica, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.ConsultandoLiquidacao)
        {
            return Ignorar($"StatusLiquidacao ({status})", agora);
        }

        var descricao = $"StatusLiquidacao recebido do banco externo: {status}";
        switch (status)
        {
            case StatusLiquidacaoExterna.Liquidada:
                Concluir(descricao, agora);
                break;
            case StatusLiquidacaoExterna.NaoLiquidada:
                AnotarPasso(descricao, agora);
                PedirEstorno(MotivoTransferencia.SemRespostaDoBanco, politica, agora);
                break;
            case StatusLiquidacaoExterna.Recusada:
                AnotarPasso(descricao, agora);
                PedirEstorno(MotivoTransferencia.RecusadaPeloBanco, politica, agora);
                break;
            default:
                throw new UnreachableException($"Status {status} sem tratamento.");
        }

        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarEstornoRealizado(DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.AguardandoEstorno)
        {
            return Ignorar("EstornoRealizado", agora);
        }

        Cancelar(Motivo!.Value, "EstornoRealizado recebido de Contas", agora);
        return ResultadoPasso.Aplicado;
    }

    /// <summary>
    /// Chamado pela varredura. Com o prazo vencido, reenvia o comando do passo (id de mensagem novo; os
    /// participantes deduplicam pelo id da transferência) ou, esgotados os reenvios, segue o caminho de falha.
    /// </summary>
    public ResultadoPasso VerificarPrazo(PoliticaSaga politica, DateTimeOffset agora)
    {
        if (PrazoDoPasso is not { } prazo || prazo > agora)
        {
            return ResultadoPasso.Ignorado;
        }

        if (Estado == EstadoTransferencia.AguardandoEstorno || TentativasNoPasso <= politica.ReenviosPorPasso)
        {
            TentativasNoPasso++;
            PrazoDoPasso = agora + politica.PrazoDe(Estado);
            AnotarPasso($"Prazo vencido: {NomeDoComando} reenviado (tentativa {TentativasNoPasso})", agora);
            EnviarComandoDoPasso(agora);
            return ResultadoPasso.Aplicado;
        }

        switch (Estado)
        {
            case EstadoTransferencia.AguardandoDebito:
                DebitoSemResposta = true;
                if (CancelamentoSolicitado)
                {
                    Cancelar(MotivoTransferencia.CanceladaPeloCliente, "Contas não respondeu ao débito; cancelamento do cliente concluído", agora);
                }
                else
                {
                    Recusar(MotivoTransferencia.SemRespostaDeContas, "Contas não respondeu ao débito depois dos reenvios", agora);
                }

                break;
            case EstadoTransferencia.AguardandoLiquidacao:
                AnotarPasso("O banco externo não respondeu à liquidação depois dos reenvios", agora);
                EntrarNoPasso(EstadoTransferencia.ConsultandoLiquidacao, politica, agora);
                break;
            case EstadoTransferencia.ConsultandoLiquidacao:
                AnotarPasso("O banco externo não respondeu às consultas", agora);
                PedirEstorno(MotivoTransferencia.SemRespostaDoBanco, politica, agora);
                break;
            default:
                throw new UnreachableException($"O estado {Estado} não tem prazo.");
        }

        return ResultadoPasso.Aplicado;
    }

    /// <summary>
    /// Cancelamento pelo cliente, possível até a liquidação ser pedida. Com o débito em andamento, só anota: a
    /// resposta do débito decide entre estornar e terminar direto. Repetir o pedido não muda nada.
    /// </summary>
    /// <exception cref="ExcecaoCancelamentoNaoPermitido">A liquidação já foi pedida ou a saga já terminou.</exception>
    public void SolicitarCancelamento(DateTimeOffset agora)
    {
        switch (Estado)
        {
            case EstadoTransferencia.Agendada:
                Cancelar(MotivoTransferencia.CanceladaPeloCliente, "Cancelada pelo cliente antes de começar", agora);
                break;
            case EstadoTransferencia.AguardandoDebito when !CancelamentoSolicitado:
                CancelamentoSolicitado = true;
                AnotarPasso("Cancelamento pedido pelo cliente; a resposta do débito decide se é preciso estornar", agora);
                break;
            case EstadoTransferencia.AguardandoDebito:
                break;
            case EstadoTransferencia.AguardandoEstorno or EstadoTransferencia.Cancelada
                when Motivo == MotivoTransferencia.CanceladaPeloCliente:
                break;
            default:
                throw new ExcecaoCancelamentoNaoPermitido(Estado);
        }
    }

    private bool AguardaOBanco =>
        Estado is EstadoTransferencia.AguardandoLiquidacao or EstadoTransferencia.ConsultandoLiquidacao;

    private string NomeDoComando => ComandoDe(Estado);

    private void PedirEstorno(MotivoTransferencia motivo, PoliticaSaga politica, DateTimeOffset agora)
    {
        Motivo = motivo;
        EntrarNoPasso(EstadoTransferencia.AguardandoEstorno, politica, agora);
    }

    /// <summary>Passo que espera resposta: envia o comando pela primeira vez e abre o prazo.</summary>
    private void EntrarNoPasso(EstadoTransferencia estado, PoliticaSaga politica, DateTimeOffset agora)
    {
        TentativasNoPasso = 1;
        PrazoDoPasso = agora + politica.PrazoDe(estado);
        MudarPara(estado, $"{ComandoDe(estado)} enviado (tentativa 1)", agora);
        EnviarComandoDoPasso(agora);
    }

    private static string ComandoDe(EstadoTransferencia estado) => estado switch
    {
        EstadoTransferencia.AguardandoDebito => "DebitarTransferenciaExterna",
        EstadoTransferencia.AguardandoLiquidacao => "LiquidarTransferencia",
        EstadoTransferencia.ConsultandoLiquidacao => "ConsultarLiquidacao",
        EstadoTransferencia.AguardandoEstorno => "EstornarTransferencia",
        _ => throw new UnreachableException($"O estado {estado} não envia comando."),
    };

    private void EnviarComandoDoPasso(DateTimeOffset agora)
    {
        IEventoDeTransferencia comando = Estado switch
        {
            EstadoTransferencia.AguardandoDebito => new DebitoSolicitado(
                Id, ContaOrigemId, Valor, Tarifa, IbanDestino.Valor, NomeBeneficiario, Descricao, TentativasNoPasso, agora),
            EstadoTransferencia.AguardandoLiquidacao => new LiquidacaoSolicitada(
                Id, IbanDestino.Valor, Bic, NomeBeneficiario, Valor, TentativasNoPasso, agora),
            EstadoTransferencia.ConsultandoLiquidacao => new ConsultaLiquidacaoSolicitada(Id, TentativasNoPasso, agora),
            EstadoTransferencia.AguardandoEstorno => new EstornoSolicitado(Id, ContaOrigemId, TentativasNoPasso, agora),
            _ => throw new UnreachableException($"O estado {Estado} não envia comando."),
        };

        Registrar(comando);
    }

    private void Concluir(string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Concluida, motivo: null, descricao, agora);
        Registrar(new LiberacaoTravaSolicitada(Id, ContaOrigemId, agora));
        Registrar(new TransferenciaConcluida(Id, ContaOrigemId, Valor, Tarifa, agora));
    }

    private void Cancelar(MotivoTransferencia motivo, string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Cancelada, motivo, descricao, agora);
        Registrar(new TransferenciaCancelada(Id, ContaOrigemId, motivo, agora));
    }

    private void Recusar(MotivoTransferencia motivo, string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Recusada, motivo, descricao, agora);
        Registrar(new TransferenciaRecusada(Id, ContaOrigemId, motivo, agora));
    }

    private void Encerrar(EstadoTransferencia estado, MotivoTransferencia? motivo, string descricao, DateTimeOffset agora)
    {
        Motivo = motivo;
        TentativasNoPasso = 0;
        PrazoDoPasso = null;
        MudarPara(estado, descricao, agora);
    }

    private ResultadoPasso Ignorar(string resposta, DateTimeOffset agora)
    {
        AnotarPasso($"{resposta} ignorado: não cabe no estado {Estado}", agora);
        return ResultadoPasso.Ignorado;
    }

    private void MudarPara(EstadoTransferencia estado, string descricao, DateTimeOffset agora)
    {
        Estado = estado;
        AnotarPasso(descricao, agora);
    }

    private void AnotarPasso(string descricao, DateTimeOffset agora)
    {
        Versao++;
        AtualizadaEm = agora;
        _historico.Add(new PassoTransferencia(Versao, Estado, descricao, agora));
    }
}
