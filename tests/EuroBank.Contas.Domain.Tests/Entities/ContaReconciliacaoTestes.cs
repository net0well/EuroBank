using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.Entities;

/// <summary>Reconciliação (débito mesmo sem saldo), saldo negativo e devolução pelo banco externo.</summary>
public sealed class ContaReconciliacaoTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Conta que debitou 100 + 2,50 numa transferência e depois foi estornada.</summary>
    private static (Conta Conta, Guid TransferenciaId) EstornadaCom(decimal saldoFinal)
    {
        var conta = Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora);
        conta.Depositar(Dinheiro.De(102.50m), Agora);
        var transferenciaId = Guid.CreateVersion7();
        conta.DebitarTransferenciaExterna(transferenciaId, Dinheiro.De(100m), Dinheiro.De(2.50m), "DE89370400440532013095", "Bruno", null, Agora);
        conta.EstornarTransferencia(transferenciaId, Agora);
        var excedente = 102.50m - saldoFinal;
        if (excedente > 0)
        {
            conta.Sacar(Dinheiro.De(excedente), Agora);
        }

        conta.LimparEventos();
        return (conta, transferenciaId);
    }

    private static (Conta Conta, Guid TransferenciaId) Concluida()
    {
        var conta = Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora);
        conta.Depositar(Dinheiro.De(1_000m), Agora);
        var transferenciaId = Guid.CreateVersion7();
        conta.DebitarTransferenciaExterna(transferenciaId, Dinheiro.De(100m), Dinheiro.De(2.50m), "DE89370400440532013094", "Bruno", null, Agora);
        conta.LiberarTrava(transferenciaId);
        conta.LimparEventos();
        return (conta, transferenciaId);
    }

    [Fact]
    public void Reconciliacao_DebitaValorETarifaMesmoSemSaldo_EAContaFicaNegativa()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 50m);

        conta.DebitarReconciliacao(transferenciaId, Agora).ShouldBe(ResultadoOperacao.Aplicada);

        conta.Saldo.ShouldBe(Saldo.De(-52.50m, conta.Moeda));
        conta.Saldo.EhNegativo.ShouldBeTrue();
        conta.Transferencias.ShouldHaveSingleItem().Status.ShouldBe(StatusTransferenciaDaConta.Reconciliada);
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<DebitoReconciliacao>();
        evento.Valor.ShouldBe(Dinheiro.De(102.50m));
        evento.SaldoApos.ShouldBe(Saldo.De(-52.50m, conta.Moeda));
    }

    [Fact]
    public void Reconciliacao_Repetida_NaoDebitaDeNovo()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 50m);
        conta.DebitarReconciliacao(transferenciaId, Agora);
        conta.LimparEventos();

        conta.DebitarReconciliacao(transferenciaId, Agora).ShouldBe(ResultadoOperacao.JaProcessada);

        conta.EventosDominio.ShouldBeEmpty();
        conta.Saldo.ShouldBe(Saldo.De(-52.50m, conta.Moeda));
    }

    [Fact]
    public void Reconciliacao_DeTransferenciaQueNaoFoiEstornada_Recusa()
    {
        var (conta, transferenciaId) = Concluida();

        Should.Throw<ExcecaoTransferenciaNaoEstornada>(() => conta.DebitarReconciliacao(transferenciaId, Agora));
    }

    [Fact]
    public void Reconciliacao_EmContaEncerrada_Recusa()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 0m);
        conta.Encerrar(Agora);

        Should.Throw<ExcecaoContaEncerrada>(() => conta.DebitarReconciliacao(transferenciaId, Agora));
    }

    [Fact]
    public void ComSaldoNegativo_SaqueETransferenciaSaoRecusados_DepositoEAceito()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 50m);
        conta.DebitarReconciliacao(transferenciaId, Agora);

        Should.Throw<ExcecaoSaldoNegativo>(() => conta.Sacar(Dinheiro.De(1m), Agora));
        Should.Throw<ExcecaoSaldoNegativo>(() => conta.DebitarTransferenciaExterna(
            Guid.CreateVersion7(), Dinheiro.De(1m), Dinheiro.De(2.50m), "DE89370400440532013000", "Bruno", null, Agora));

        conta.Depositar(Dinheiro.De(60m), Agora);
        conta.Saldo.ShouldBe(Saldo.De(7.50m, conta.Moeda));
    }

    [Fact]
    public void ComSaldoNegativo_NaoPodeEncerrar()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 0m);
        conta.DebitarReconciliacao(transferenciaId, Agora);

        Should.Throw<ExcecaoContaComSaldo>(() => conta.Encerrar(Agora));
    }

    [Fact]
    public void Devolucao_CreditaSoOValorSemATarifa()
    {
        var (conta, transferenciaId) = Concluida();

        conta.CreditarDevolucao(transferenciaId, Agora).ShouldBe(ResultadoOperacao.Aplicada);

        conta.Saldo.ShouldBe(Saldo.De(997.50m, conta.Moeda));
        conta.Transferencias.ShouldHaveSingleItem().Status.ShouldBe(StatusTransferenciaDaConta.Devolvida);
        conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<DevolucaoTransferenciaExterna>().Valor.ShouldBe(Dinheiro.De(100m));
    }

    [Fact]
    public void Devolucao_Repetida_CreditaUmaVezSo()
    {
        var (conta, transferenciaId) = Concluida();
        conta.CreditarDevolucao(transferenciaId, Agora);

        conta.CreditarDevolucao(transferenciaId, Agora).ShouldBe(ResultadoOperacao.JaProcessada);

        conta.Saldo.ShouldBe(Saldo.De(997.50m, conta.Moeda));
    }

    [Fact]
    public void Devolucao_DeTransferenciaEstornada_Recusa()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 102.50m);

        Should.Throw<ExcecaoTransferenciaNaoConcluida>(() => conta.CreditarDevolucao(transferenciaId, Agora));
    }

    [Fact]
    public void Devolucao_EmContaEncerrada_Recusa()
    {
        var (conta, transferenciaId) = Concluida();
        conta.Sacar(Dinheiro.De(897.50m), Agora);
        conta.Encerrar(Agora);

        Should.Throw<ExcecaoContaEncerrada>(() => conta.CreditarDevolucao(transferenciaId, Agora));
    }

    [Fact]
    public void Devolucao_DepoisDaReconciliacao_CreditaOValor()
    {
        var (conta, transferenciaId) = EstornadaCom(saldoFinal: 0m);
        conta.DebitarReconciliacao(transferenciaId, Agora);

        conta.CreditarDevolucao(transferenciaId, Agora).ShouldBe(ResultadoOperacao.Aplicada);

        conta.Saldo.ShouldBe(Saldo.De(-2.50m, conta.Moeda));
    }
}
