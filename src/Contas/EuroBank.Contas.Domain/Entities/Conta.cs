using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Entities;

/// <summary>
/// Agregado Conta. Ciclo de vida: Ativa, Bloqueada (só recebe créditos) e Encerrada (final, só com saldo zero).
/// Toda operação bem-sucedida incrementa <see cref="Versao"/> e registra exatamente um evento de domínio.
/// </summary>
public sealed class Conta : RaizDeAgregado<ContaId>
{
    private Conta()
    {
        // Usado pelo EF Core.
    }

    public Iban Iban { get; private set; } = null!;

    public Titular Titular { get; private set; } = null!;

    public Dinheiro Saldo { get; private set; } = Dinheiro.Zero;

    public StatusConta Status { get; private set; }

    /// <summary>Versão de negócio da conta; também é o token de concorrência otimista.</summary>
    public int Versao { get; private set; }

    public DateTimeOffset AbertaEm { get; private set; }

    /// <summary>
    /// Transferências pedidas por uma saga e já debitadas. Servem a dois propósitos: idempotência (o mesmo
    /// comando repetido não debita nem estorna duas vezes) e trava semântica (as pendentes impedem o encerramento,
    /// para a compensação nunca encontrar a conta encerrada).
    /// </summary>
    public IReadOnlyList<TransferenciaDaConta> Transferencias => _transferencias.AsReadOnly();

    public bool TemTransferenciaEmAndamento =>
        _transferencias.Exists(transferencia => transferencia.Status == StatusTransferenciaDaConta.Pendente);

    private readonly List<TransferenciaDaConta> _transferencias = [];

    public static Conta Abrir(Iban iban, Titular titular, DateTimeOffset agora)
    {
        var conta = new Conta
        {
            Id = ContaId.Nova(),
            Iban = iban,
            Titular = titular,
            Saldo = Dinheiro.Zero,
            Status = StatusConta.Ativa,
            Versao = 1,
            AbertaEm = agora,
        };

        conta.Registrar(new ContaAberta(conta.Id, iban, titular, conta.Versao, agora));
        return conta;
    }

    public void Depositar(Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeReceber(valor);

        Saldo += valor;
        Versao++;
        Registrar(new DepositoRealizado(Id, valor, Saldo, Versao, agora));
    }

    public void Sacar(Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeDebitar(valor);

        Saldo -= valor;
        Versao++;
        Registrar(new SaqueRealizado(Id, valor, Saldo, Versao, agora));
    }

    public void Bloquear(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (Status == StatusConta.Bloqueada)
        {
            throw new ExcecaoContaJaBloqueada();
        }

        Status = StatusConta.Bloqueada;
        Versao++;
        Registrar(new ContaBloqueada(Id, Versao, agora));
    }

    public void Desbloquear(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (Status != StatusConta.Bloqueada)
        {
            throw new ExcecaoContaNaoBloqueada();
        }

        Status = StatusConta.Ativa;
        Versao++;
        Registrar(new ContaDesbloqueada(Id, Versao, agora));
    }

    public void Encerrar(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (!Saldo.EhZero)
        {
            throw new ExcecaoContaComSaldo(Saldo);
        }

        if (TemTransferenciaEmAndamento)
        {
            throw new ExcecaoContaComTransferenciaEmAndamento();
        }

        Status = StatusConta.Encerrada;
        Versao++;
        Registrar(new ContaEncerrada(Id, Versao, agora));
    }

    /// <summary>
    /// Primeiro passo da saga de transferência externa: debita valor e tarifa e trava a transferência.
    /// Repetir o comando (reenvio do orquestrador) não debita de novo.
    /// </summary>
    public ResultadoOperacao DebitarTransferenciaExterna(
        Guid transferenciaId,
        Dinheiro valor,
        Dinheiro tarifa,
        string ibanDestino,
        string nomeBeneficiario,
        string? descricao,
        DateTimeOffset agora)
    {
        if (BuscarTransferencia(transferenciaId) is not null)
        {
            return ResultadoOperacao.JaProcessada;
        }

        GarantirValorPositivo(valor);
        var total = valor + tarifa;
        GarantirPodeDebitar(total);

        Saldo -= total;
        Versao++;
        _transferencias.Add(new TransferenciaDaConta(transferenciaId, valor, tarifa, nomeBeneficiario, agora));
        Registrar(new TransferenciaExternaEnviada(
            Id, transferenciaId, ibanDestino, nomeBeneficiario, descricao, valor, tarifa, Saldo, Versao, agora));
        return ResultadoOperacao.Aplicada;
    }

    /// <summary>Compensação da saga: devolve valor e tarifa. É crédito, então vale também em conta bloqueada.</summary>
    public ResultadoOperacao EstornarTransferencia(Guid transferenciaId, DateTimeOffset agora)
    {
        var transferencia = BuscarTransferencia(transferenciaId) ?? throw new ExcecaoTransferenciaDesconhecida(transferenciaId);
        switch (transferencia.Status)
        {
            case StatusTransferenciaDaConta.Estornada:
                return ResultadoOperacao.JaProcessada;
            case StatusTransferenciaDaConta.Liberada:
                throw new ExcecaoTransferenciaJaConcluida(transferenciaId);
        }

        var total = transferencia.Valor + transferencia.Tarifa;
        Saldo += total;
        Versao++;
        transferencia.Estornar(agora);
        Registrar(new EstornoTransferencia(Id, transferenciaId, transferencia.NomeBeneficiario, total, Saldo, Versao, agora));
        return ResultadoOperacao.Aplicada;
    }

    /// <summary>
    /// Fim feliz da saga: a transferência sai da trava. Não mexe no saldo nem na versão e não gera evento,
    /// porque nada que o Extrato precise saber mudou.
    /// </summary>
    public ResultadoOperacao LiberarTrava(Guid transferenciaId)
    {
        var transferencia = BuscarTransferencia(transferenciaId) ?? throw new ExcecaoTransferenciaDesconhecida(transferenciaId);
        if (transferencia.Status != StatusTransferenciaDaConta.Pendente)
        {
            return ResultadoOperacao.JaProcessada;
        }

        transferencia.Liberar();
        return ResultadoOperacao.Aplicada;
    }

    private TransferenciaDaConta? BuscarTransferencia(Guid transferenciaId) =>
        _transferencias.Find(transferencia => transferencia.Id == transferenciaId);

    // As operações de transferência são internas: só o ServicoTransferencia as coordena,
    // garantindo que débito e crédito aconteçam juntos.

    internal void GarantirPodeEnviar(Dinheiro valor) => GarantirPodeDebitar(valor);

    internal void GarantirPodeReceber(Dinheiro valor)
    {
        GarantirValorPositivo(valor);
        GarantirNaoEncerrada();
    }

    internal void EnviarTransferencia(Guid transferenciaId, Iban destino, Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeEnviar(valor);

        Saldo -= valor;
        Versao++;
        Registrar(new TransferenciaEnviada(Id, transferenciaId, destino, valor, Saldo, Versao, agora));
    }

    internal void ReceberTransferencia(Guid transferenciaId, Iban origem, Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeReceber(valor);

        Saldo += valor;
        Versao++;
        Registrar(new TransferenciaRecebida(Id, transferenciaId, origem, valor, Saldo, Versao, agora));
    }

    private void GarantirPodeDebitar(Dinheiro valor)
    {
        GarantirValorPositivo(valor);
        GarantirNaoEncerrada();
        if (Status == StatusConta.Bloqueada)
        {
            throw new ExcecaoContaBloqueada();
        }

        if (valor > Saldo)
        {
            throw new ExcecaoSaldoInsuficiente(Saldo, valor);
        }
    }

    private void GarantirNaoEncerrada()
    {
        if (Status == StatusConta.Encerrada)
        {
            throw new ExcecaoContaEncerrada();
        }
    }

    private static void GarantirValorPositivo(Dinheiro valor)
    {
        if (valor.EhZero)
        {
            throw new ExcecaoValorInvalido("o valor da operação deve ser maior que zero");
        }
    }
}
