using System.Diagnostics;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.Policies;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Entities;

/// <summary>
/// Orquestrador das sagas de transferência, como agregado. Guarda em que passo a saga está, decide o próximo
/// comando a cada resposta ou prazo vencido e registra a decisão como evento (que a outbox transforma no comando
/// para Contas, para o câmbio ou para o banco externo). Respostas que não cabem no estado atual são ignoradas.
/// </summary>
/// <remarks>
/// Externa: Agendada, [Cotando], AguardandoDebito, Debitada, AguardandoLiquidacao, [ConsultandoLiquidacao],
/// Concluida e, se o banco devolver depois, AguardandoCreditoDevolucao e Devolvida. Câmbio interno: Agendada,
/// Cotando, AguardandoDebito, Debitada, AguardandoCredito, Concluida. Falha depois do débito: AguardandoEstorno e
/// Cancelada. Falha antes do débito: Recusada. Confirmação do banco depois do estorno: Divergente e, com a
/// reconciliação pedida pela API, Reconciliando e Concluida (Reconciliada).
/// </remarks>
public sealed class Transferencia : RaizDeAgregado<TransferenciaId>
{
    private readonly List<PassoTransferencia> _historico = [];

    // Valores persistidos só como números; a moeda de cada um está numa coluna própria.
    private decimal _valor;
    private decimal _tarifa;
    private decimal? _valorDebitado;
    private decimal? _valorDestino;
    private Guid? _cotacaoId;
    private decimal? _cotacaoTaxa;
    private DateTimeOffset? _cotacaoValidaAte;

    private Transferencia()
    {
        // Usado pelo EF Core.
    }

    public TipoTransferencia Tipo { get; private set; }

    /// <summary>Id da conta no contexto Contas (outro bounded context, por isso só o Guid).</summary>
    public Guid ContaOrigemId { get; private set; }

    public string IbanOrigem { get; private set; } = string.Empty;

    /// <summary>Titular da conta de origem (vai no crédito do câmbio interno).</summary>
    public string NomeOrigem { get; private set; } = string.Empty;

    public Moeda MoedaOrigem { get; private set; }

    /// <summary>Só no câmbio interno: a conta EuroBank que recebe.</summary>
    public Guid? ContaDestinoId { get; private set; }

    public IbanDestino IbanDestino { get; private set; } = null!;

    /// <summary>Só na externa: o banco de destino.</summary>
    public string? Bic { get; private set; }

    public string NomeBeneficiario { get; private set; } = string.Empty;

    public Moeda MoedaDestino { get; private set; }

    public Moeda MoedaDoValor { get; private set; }

    /// <summary>O valor pedido, na moeda em que o cliente o informou (a de origem ou a de destino).</summary>
    public Dinheiro Valor => Dinheiro.De(_valor, MoedaDoValor);

    public Dinheiro Tarifa => Dinheiro.De(_tarifa, MoedaOrigem);

    /// <summary>Quanto sai da conta de origem, sem a tarifa. Com câmbio, só existe depois da cotação.</summary>
    public Dinheiro? ValorDebitado => _valorDebitado is { } valor ? Dinheiro.De(valor, MoedaOrigem) : null;

    /// <summary>Quanto o destino recebe. Com câmbio, só existe depois da cotação.</summary>
    public Dinheiro? ValorDestino => _valorDestino is { } valor ? Dinheiro.De(valor, MoedaDestino) : null;

    public CotacaoTravada? Cotacao =>
        _cotacaoId is { } id ? new CotacaoTravada(id, _cotacaoTaxa!.Value, _cotacaoValidaAte!.Value) : null;

    public bool PrecisaDeCambio => MoedaOrigem != MoedaDestino;

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

    /// <summary>
    /// O banco confirmou a liquidação com o estorno já pedido (o comando está com Contas e não volta): quando o
    /// estorno terminar, a transferência fica Divergente em vez de Cancelada.
    /// </summary>
    public bool LiquidacaoConfirmadaNoEstorno { get; private set; }

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

    /// <summary>
    /// Para um IBAN de outro banco; com moeda de destino diferente da conta, passa pelo câmbio. Em EUR é SEPA: só para
    /// países da área SEPA e sujeita ao horário de corte.
    /// </summary>
    public static Transferencia SolicitarExterna(
        ContaEuroBank origem,
        IbanDestino ibanDestino,
        string? bic,
        string? nomeBeneficiario,
        Dinheiro valor,
        Moeda moedaDestino,
        string? descricao,
        string? emailNotificacao,
        string idempotencyKey,
        string hashDoPedido,
        DateTimeOffset? executarEm,
        DateTimeOffset agora,
        PoliticaSepa? sepa = null)
    {
        if (ibanDestino.EhEuroBank)
        {
            throw new ExcecaoDestinoEuroBank();
        }

        var bicNormalizado = ValueObjects.Bic.Normalizar(bic);

        if (string.IsNullOrWhiteSpace(nomeBeneficiario))
        {
            throw new ExcecaoBeneficiarioObrigatorio();
        }

        var emEuro = moedaDestino == Moeda.EUR;
        if (emEuro && !PaisesSepa.Contem(ibanDestino.Pais))
        {
            throw new ExcecaoPaisForaDaSepa(ibanDestino.Pais);
        }

        var transferencia = Criar(
            TipoTransferencia.Externa,
            origem,
            contaDestinoId: null,
            ibanDestino,
            bicNormalizado,
            nomeBeneficiario.Trim(),
            valor,
            moedaDestino,
            descricao,
            emailNotificacao,
            idempotencyKey,
            hashDoPedido,
            executarEm,
            agora);

        if (emEuro)
        {
            transferencia.ExecutarEmEfetivo = (sepa ?? PoliticaSepa.Padrao).ExecucaoEfetiva(transferencia.ExecutarEmEfetivo);
        }

        return transferencia;
    }

    /// <summary>Entre contas EuroBank de moedas diferentes: sempre passa pelo câmbio.</summary>
    public static Transferencia SolicitarCambioInterno(
        ContaEuroBank origem,
        ContaEuroBank destino,
        Dinheiro valor,
        string? descricao,
        string? emailNotificacao,
        string idempotencyKey,
        string hashDoPedido,
        DateTimeOffset? executarEm,
        DateTimeOffset agora)
    {
        if (destino.Id == origem.Id)
        {
            throw new ExcecaoDestinoIgualOrigem();
        }

        if (destino.Moeda == origem.Moeda)
        {
            throw new ExcecaoCambioNaMesmaMoeda();
        }

        var ibanDestino = IbanDestino.Criar(destino.Iban);
        if (!ibanDestino.EhEuroBank)
        {
            throw new ExcecaoDestinoNaoEuroBank();
        }

        return Criar(
            TipoTransferencia.CambioInterno,
            origem,
            destino.Id,
            ibanDestino,
            bic: null,
            destino.Titular,
            valor,
            destino.Moeda,
            descricao,
            emailNotificacao,
            idempotencyKey,
            hashDoPedido,
            executarEm,
            agora);
    }

    /// <summary>
    /// Começa a saga na hora marcada. Com câmbio, pede a cotação (o limite é conferido quando ela chega, porque só
    /// então se sabe quanto sai da conta); sem câmbio, confere o limite e pede o débito. Ignorado fora da hora ou
    /// se já começou (a API e a varredura podem tentar ao mesmo tempo).
    /// </summary>
    public ResultadoPasso Iniciar(Dinheiro jaTransferidoNoDia, PoliticaSaga politica, DateTimeOffset agora)
    {
        if (!EstaNaHora(agora))
        {
            return ResultadoPasso.Ignorado;
        }

        if (PrecisaDeCambio)
        {
            EntrarNoPasso(EstadoTransferencia.Cotando, politica, agora);
        }
        else
        {
            PedirDebitoDentroDoLimite(jaTransferidoNoDia, politica, agora);
        }

        return ResultadoPasso.Aplicado;
    }

    /// <summary>
    /// Cotação recebida: trava a taxa e os dois valores, confere o limite com o valor que vai sair da conta e pede o
    /// débito. Uma cotação que já chegou vencida é descartada e a saga cota de novo.
    /// </summary>
    public ResultadoPasso RegistrarCotacaoEmitida(
        CotacaoTravada cotacao,
        Dinheiro valorOrigem,
        Dinheiro valorDestino,
        Dinheiro jaTransferidoNoDia,
        PoliticaSaga politica,
        DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.Cotando)
        {
            return Ignorar("CotacaoEmitida", agora);
        }

        if (valorOrigem.Moeda != MoedaOrigem || valorDestino.Moeda != MoedaDestino)
        {
            throw new ExcecaoMoedaDiferente(MoedaOrigem, valorOrigem.Moeda);
        }

        if (cotacao.ValidaAte <= agora)
        {
            AnotarPasso($"CotacaoEmitida {cotacao.Id} chegou vencida: cotando de novo", agora);
            EntrarNoPasso(EstadoTransferencia.Cotando, politica, agora);
            return ResultadoPasso.Aplicado;
        }

        _cotacaoId = cotacao.Id;
        _cotacaoTaxa = cotacao.Taxa;
        _cotacaoValidaAte = cotacao.ValidaAte;
        _valorDebitado = valorOrigem.Valor;
        _valorDestino = valorDestino.Valor;
        AnotarPasso($"CotacaoEmitida recebida do câmbio: 1 {MoedaOrigem} = {cotacao.Taxa} {MoedaDestino}", agora);

        PedirDebitoDentroDoLimite(jaTransferidoNoDia, politica, agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarCotacaoIndisponivel(string motivo, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.Cotando)
        {
            return Ignorar("CotacaoIndisponivel", agora);
        }

        Recusar(MotivoTransferencia.CambioIndisponivel, $"CotacaoIndisponivel recebida do câmbio: {motivo}", agora);
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
                EntrarNoPasso(
                    Tipo == TipoTransferencia.Externa ? EstadoTransferencia.AguardandoLiquidacao : EstadoTransferencia.AguardandoCredito,
                    politica,
                    agora);
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

    public ResultadoPasso RegistrarCreditoRealizado(DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.AguardandoCredito)
        {
            return Ignorar("CreditoRealizado", agora);
        }

        Concluir("CreditoRealizado recebido de Contas", agora);
        return ResultadoPasso.Aplicado;
    }

    /// <summary>A conta de destino recusou o crédito (encerrada, por exemplo): a origem é estornada.</summary>
    public ResultadoPasso RegistrarCreditoRecusado(MotivoTransferencia motivoDeContas, PoliticaSaga politica, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.AguardandoCredito)
        {
            return Ignorar("CreditoRecusado", agora);
        }

        AnotarPasso($"CreditoRecusado recebido de Contas: {motivoDeContas}", agora);
        PedirEstorno(
            motivoDeContas == MotivoTransferencia.ContaEncerrada ? MotivoTransferencia.DestinoEncerrado : MotivoTransferencia.RecusadaPorContas,
            politica,
            agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarLiquidacaoConfirmada(DateTimeOffset agora)
    {
        if (!AguardaOBanco)
        {
            return RegistrarLiquidacaoTardia("LiquidacaoConfirmada", agora);
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
            return status == StatusLiquidacaoExterna.Liquidada
                ? RegistrarLiquidacaoTardia($"StatusLiquidacao ({status})", agora)
                : Ignorar($"StatusLiquidacao ({status})", agora);
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

        if (LiquidacaoConfirmadaNoEstorno)
        {
            Divergir(
                MotivoTransferencia.ConfirmadaAposEstorno,
                "EstornoRealizado recebido de Contas, mas o banco já tinha confirmado a liquidação",
                agora);
        }
        else
        {
            Cancelar(Motivo!.Value, "EstornoRealizado recebido de Contas", agora);
        }

        return ResultadoPasso.Aplicado;
    }

    /// <summary>
    /// Pedido pela API para uma Divergente por confirmação tardia: Contas debita de novo valor e tarifa, mesmo que a
    /// conta fique negativa. Repetir com a reconciliação em andamento ou concluída não muda nada.
    /// </summary>
    /// <exception cref="ExcecaoReconciliacaoNaoPermitida">A transferência não está Divergente por confirmação tardia.</exception>
    public void SolicitarReconciliacao(PoliticaSaga politica, DateTimeOffset agora)
    {
        switch (Estado)
        {
            case EstadoTransferencia.Divergente when Motivo == MotivoTransferencia.ConfirmadaAposEstorno:
                AnotarPasso("Reconciliação pedida: valor e tarifa vão ser debitados de novo", agora);
                EntrarNoPasso(EstadoTransferencia.Reconciliando, politica, agora);
                break;
            case EstadoTransferencia.Reconciliando:
            case EstadoTransferencia.Concluida when Motivo == MotivoTransferencia.Reconciliada:
                break;
            default:
                throw new ExcecaoReconciliacaoNaoPermitida(Estado, Motivo);
        }
    }

    /// <summary>Sem LiberarTrava: o estorno já tirou a transferência da trava da conta.</summary>
    public ResultadoPasso RegistrarReconciliacaoRealizada(DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.Reconciliando)
        {
            return Ignorar("ReconciliacaoRealizada", agora);
        }

        Encerrar(
            EstadoTransferencia.Concluida,
            MotivoTransferencia.Reconciliada,
            "ReconciliacaoRealizada recebida de Contas: valor e tarifa debitados de novo",
            agora);
        Registrar(new TransferenciaConcluida(Id, ContaOrigemId, ValorDebitado!, Tarifa, Motivo, Resumir(), agora));
        return ResultadoPasso.Aplicado;
    }

    /// <summary>Contas recusou (conta encerrada, por exemplo): continua Divergente, para outra tentativa ou tratamento manual.</summary>
    public ResultadoPasso RegistrarReconciliacaoRecusada(MotivoTransferencia motivoDeContas, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.Reconciliando)
        {
            return Ignorar("ReconciliacaoRecusada", agora);
        }

        Encerrar(
            EstadoTransferencia.Divergente,
            MotivoTransferencia.ConfirmadaAposEstorno,
            $"ReconciliacaoRecusada recebida de Contas: {motivoDeContas}",
            agora);
        return ResultadoPasso.Aplicado;
    }

    /// <summary>O banco externo devolveu uma transferência concluída: pede a Contas o crédito do valor na origem.</summary>
    public ResultadoPasso RegistrarDevolucaoRecebida(PoliticaSaga politica, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.Concluida || Tipo != TipoTransferencia.Externa)
        {
            return Ignorar("TransferenciaDevolvida", agora);
        }

        AnotarPasso("TransferenciaDevolvida recebida do banco externo", agora);
        EntrarNoPasso(EstadoTransferencia.AguardandoCreditoDevolucao, politica, agora);
        return ResultadoPasso.Aplicado;
    }

    public ResultadoPasso RegistrarDevolucaoCreditada(DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.AguardandoCreditoDevolucao)
        {
            return Ignorar("DevolucaoCreditada", agora);
        }

        Encerrar(
            EstadoTransferencia.Devolvida,
            MotivoTransferencia.DevolvidaPeloBanco,
            "DevolucaoCreditada recebida de Contas: valor de volta na conta, sem a tarifa",
            agora);
        Registrar(new TransferenciaDevolvida(Id, ContaOrigemId, ValorDebitado!, Resumir(), agora));
        return ResultadoPasso.Aplicado;
    }

    /// <summary>O valor voltou do banco, mas a conta não pôde recebê-lo (encerrada): fica Divergente.</summary>
    public ResultadoPasso RegistrarDevolucaoRecusada(MotivoTransferencia motivoDeContas, DateTimeOffset agora)
    {
        if (Estado != EstadoTransferencia.AguardandoCreditoDevolucao)
        {
            return Ignorar("DevolucaoRecusada", agora);
        }

        Divergir(MotivoTransferencia.DevolucaoNaoCreditada, $"DevolucaoRecusada recebida de Contas: {motivoDeContas}", agora);
        return ResultadoPasso.Aplicado;
    }

    /// <summary>
    /// Chamado pela varredura. Com o prazo vencido, reenvia o comando do passo (id de mensagem novo; os
    /// participantes deduplicam pelo id da transferência) ou, esgotados os reenvios, segue o caminho de falha.
    /// Crédito, estorno, reconciliação e crédito da devolução reenviam sem limite.
    /// </summary>
    public ResultadoPasso VerificarPrazo(PoliticaSaga politica, DateTimeOffset agora)
    {
        if (PrazoDoPasso is not { } prazo || prazo > agora)
        {
            return ResultadoPasso.Ignorado;
        }

        if (ReenviaSemLimite || TentativasNoPasso <= politica.ReenviosPorPasso)
        {
            TentativasNoPasso++;
            PrazoDoPasso = agora + politica.PrazoDe(Estado);
            AnotarPasso($"Prazo vencido: {NomeDoComando(Estado)} reenviado (tentativa {TentativasNoPasso})", agora);
            EnviarComandoDoPasso(agora);
            return ResultadoPasso.Aplicado;
        }

        switch (Estado)
        {
            case EstadoTransferencia.Cotando:
                Recusar(MotivoTransferencia.CambioIndisponivel, "O câmbio não respondeu à cotação depois dos reenvios", agora);
                break;
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
    /// Cancelamento pelo cliente, possível até a liquidação ou o crédito serem pedidos. Com o débito em andamento,
    /// só anota: a resposta do débito decide entre estornar e terminar direto. Repetir o pedido não muda nada.
    /// </summary>
    /// <exception cref="ExcecaoCancelamentoNaoPermitido">A liquidação ou o crédito já foram pedidos, ou a saga terminou.</exception>
    public void SolicitarCancelamento(DateTimeOffset agora)
    {
        switch (Estado)
        {
            case EstadoTransferencia.Agendada or EstadoTransferencia.Cotando:
                Cancelar(MotivoTransferencia.CanceladaPeloCliente, "Cancelada pelo cliente antes do débito", agora);
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

    private static Transferencia Criar(
        TipoTransferencia tipo,
        ContaEuroBank origem,
        Guid? contaDestinoId,
        IbanDestino ibanDestino,
        string? bic,
        string nomeBeneficiario,
        Dinheiro valor,
        Moeda moedaDestino,
        string? descricao,
        string? emailNotificacao,
        string idempotencyKey,
        string hashDoPedido,
        DateTimeOffset? executarEm,
        DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashDoPedido);
        if (valor.EhZero)
        {
            throw new ExcecaoValorInvalido("o valor da transferência deve ser maior que zero");
        }

        if (valor.Moeda != origem.Moeda && valor.Moeda != moedaDestino)
        {
            throw new ExcecaoMoedaDoValorInvalida(valor.Moeda, origem.Moeda, moedaDestino);
        }

        var transferencia = new Transferencia
        {
            Id = TransferenciaId.Nova(),
            Tipo = tipo,
            ContaOrigemId = origem.Id,
            IbanOrigem = origem.Iban,
            NomeOrigem = origem.Titular,
            MoedaOrigem = origem.Moeda,
            ContaDestinoId = contaDestinoId,
            IbanDestino = ibanDestino,
            Bic = bic,
            NomeBeneficiario = nomeBeneficiario,
            MoedaDestino = moedaDestino,
            MoedaDoValor = valor.Moeda,
            _valor = valor.Valor,
            _tarifa = TabelaTransferencias.Tarifa(origem.Moeda).Valor,
            Descricao = descricao,
            EmailNotificacao = emailNotificacao,
            IdempotencyKey = idempotencyKey,
            HashDoPedido = hashDoPedido,
            SolicitadaEm = agora,
            ExecutarEm = executarEm,
            ExecutarEmEfetivo = executarEm > agora ? executarEm.Value : agora,
            Estado = EstadoTransferencia.Agendada,
        };

        // Sem câmbio, o que sai da conta é o que chega ao destino.
        if (!transferencia.PrecisaDeCambio)
        {
            transferencia._valorDebitado = valor.Valor;
            transferencia._valorDestino = valor.Valor;
        }

        transferencia.AnotarPasso("Transferência solicitada", agora);
        return transferencia;
    }

    private bool AguardaOBanco =>
        Estado is EstadoTransferencia.AguardandoLiquidacao or EstadoTransferencia.ConsultandoLiquidacao;

    /// <summary>O estorno foi pedido porque o banco recusou ou não respondeu: a liquidação chegou a ser pedida.</summary>
    private bool EstornouPorCausaDoBanco =>
        Tipo == TipoTransferencia.Externa
        && Motivo is MotivoTransferencia.RecusadaPeloBanco or MotivoTransferencia.SemRespostaDoBanco;

    private bool ReenviaSemLimite =>
        Estado is EstadoTransferencia.AguardandoEstorno
            or EstadoTransferencia.AguardandoCredito
            or EstadoTransferencia.Reconciliando
            or EstadoTransferencia.AguardandoCreditoDevolucao;

    /// <summary>
    /// O banco disse que liquidou quando a saga já tinha desistido dele e pedido o estorno. Com o estorno em andamento,
    /// só anota (o comando já está com Contas); depois dele, a transferência fica Divergente.
    /// </summary>
    private ResultadoPasso RegistrarLiquidacaoTardia(string resposta, DateTimeOffset agora)
    {
        if (EstornouPorCausaDoBanco && Estado == EstadoTransferencia.AguardandoEstorno && !LiquidacaoConfirmadaNoEstorno)
        {
            LiquidacaoConfirmadaNoEstorno = true;
            AnotarPasso($"{resposta} recebido com o estorno em andamento: depois dele, a transferência fica Divergente", agora);
            return ResultadoPasso.Aplicado;
        }

        if (EstornouPorCausaDoBanco && Estado == EstadoTransferencia.Cancelada)
        {
            Divergir(
                MotivoTransferencia.ConfirmadaAposEstorno,
                $"{resposta} recebido depois do estorno: o valor chegou ao destino e também voltou à conta",
                agora);
            return ResultadoPasso.Aplicado;
        }

        return Ignorar(resposta, agora);
    }

    /// <summary>O limite diário é da moeda da conta e conta só o valor, sem a tarifa.</summary>
    private void PedirDebitoDentroDoLimite(Dinheiro jaTransferidoNoDia, PoliticaSaga politica, DateTimeOffset agora)
    {
        var limite = TabelaTransferencias.LimiteDiario(MoedaOrigem);
        if (jaTransferidoNoDia + ValorDebitado! > limite)
        {
            Recusar(MotivoTransferencia.LimiteDiarioExcedido, $"Limite diário de {limite} {MoedaOrigem} excedido", agora);
            return;
        }

        EntrarNoPasso(EstadoTransferencia.AguardandoDebito, politica, agora);
    }

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
        MudarPara(estado, $"{NomeDoComando(estado)} enviado (tentativa 1)", agora);
        EnviarComandoDoPasso(agora);
    }

    private string NomeDoComando(EstadoTransferencia estado) => estado switch
    {
        EstadoTransferencia.Cotando => "CotarCambio",
        EstadoTransferencia.AguardandoDebito when Tipo == TipoTransferencia.CambioInterno => "DebitarTransferenciaCambio",
        EstadoTransferencia.AguardandoDebito => "DebitarTransferenciaExterna",
        EstadoTransferencia.AguardandoLiquidacao => "LiquidarTransferencia",
        EstadoTransferencia.ConsultandoLiquidacao => "ConsultarLiquidacao",
        EstadoTransferencia.AguardandoCredito => "CreditarTransferenciaCambio",
        EstadoTransferencia.AguardandoEstorno => "EstornarTransferencia",
        EstadoTransferencia.Reconciliando => "DebitarReconciliacao",
        EstadoTransferencia.AguardandoCreditoDevolucao => "CreditarDevolucao",
        _ => throw new UnreachableException($"O estado {estado} não envia comando."),
    };

    private void EnviarComandoDoPasso(DateTimeOffset agora)
    {
        IEventoDeTransferencia comando = Estado switch
        {
            EstadoTransferencia.Cotando => new CotacaoSolicitada(Id, MoedaOrigem, MoedaDestino, Valor, TentativasNoPasso, agora),
            EstadoTransferencia.AguardandoDebito => new DebitoSolicitado(
                Id,
                Tipo,
                ContaOrigemId,
                ValorDebitado!,
                Tarifa,
                IbanDestino.Valor,
                NomeBeneficiario,
                Descricao,
                ValorDestino!,
                Cotacao?.Taxa,
                TentativasNoPasso,
                agora),
            EstadoTransferencia.AguardandoLiquidacao => new LiquidacaoSolicitada(
                Id, IbanDestino.Valor, Bic!, NomeBeneficiario, ValorDestino!, TentativasNoPasso, agora),
            EstadoTransferencia.ConsultandoLiquidacao => new ConsultaLiquidacaoSolicitada(Id, TentativasNoPasso, agora),
            EstadoTransferencia.AguardandoCredito => new CreditoSolicitado(
                Id,
                ContaDestinoId!.Value,
                ValorDestino!,
                IbanOrigem,
                NomeOrigem,
                Descricao,
                ValorDebitado!,
                Cotacao!.Taxa,
                TentativasNoPasso,
                agora),
            EstadoTransferencia.AguardandoEstorno => new EstornoSolicitado(Id, ContaOrigemId, TentativasNoPasso, agora),
            EstadoTransferencia.Reconciliando => new ReconciliacaoSolicitada(Id, ContaOrigemId, TentativasNoPasso, agora),
            EstadoTransferencia.AguardandoCreditoDevolucao => new CreditoDevolucaoSolicitado(Id, ContaOrigemId, TentativasNoPasso, agora),
            _ => throw new UnreachableException($"O estado {Estado} não envia comando."),
        };

        Registrar(comando);
    }

    private void Concluir(string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Concluida, motivo: null, descricao, agora);
        Registrar(new LiberacaoTravaSolicitada(Id, ContaOrigemId, agora));
        Registrar(new TransferenciaConcluida(Id, ContaOrigemId, ValorDebitado!, Tarifa, Motivo, Resumir(), agora));
    }

    private void Cancelar(MotivoTransferencia motivo, string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Cancelada, motivo, descricao, agora);
        Registrar(new TransferenciaCancelada(Id, ContaOrigemId, motivo, Resumir(), agora));
    }

    private void Divergir(MotivoTransferencia motivo, string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Divergente, motivo, descricao, agora);
        Registrar(new TransferenciaDivergente(Id, ContaOrigemId, motivo, Resumir(), agora));
    }

    private void Recusar(MotivoTransferencia motivo, string descricao, DateTimeOffset agora)
    {
        Encerrar(EstadoTransferencia.Recusada, motivo, descricao, agora);
        Registrar(new TransferenciaRecusada(Id, ContaOrigemId, motivo, Resumir(), agora));
    }

    /// <summary>O retrato que os eventos finais levam para fora do contexto.</summary>
    private ResumoTransferencia Resumir() => new(
        Tipo,
        NomeOrigem,
        NomeBeneficiario,
        IbanDestino.Valor,
        Valor,
        Tarifa,
        ValorDebitado,
        ValorDestino,
        MoedaDestino,
        Cotacao?.Taxa,
        Descricao,
        EmailNotificacao);

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
