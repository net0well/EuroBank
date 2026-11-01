
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.Services;

public sealed class ServicoTransferenciaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private readonly ServicoTransferencia _servico = new();

    private static Conta NovaConta(long sequencial, decimal saldo)
    {
        var conta = Conta.Abrir(Iban.Gerar(sequencial), Titular.Criar($"Titular {sequencial}"), Agora);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo), Agora);
        }

        conta.LimparEventos();
        return conta;
    }

    [Fact]
    public void Transferir_ComSaldo_DebitaOrigemCreditaDestinoERegistraEventosCorrelacionados()
    {
        var origem = NovaConta(1, 100m);
        var destino = NovaConta(2, 0m);

        var transferenciaId = _servico.Transferir(origem, destino, Dinheiro.De(30m), Agora);

        origem.Saldo.ShouldBe(Dinheiro.De(70m));
        destino.Saldo.ShouldBe(Dinheiro.De(30m));
        origem.Versao.ShouldBe(3);
        destino.Versao.ShouldBe(2);

        var enviada = origem.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaEnviada>();
        enviada.TransferenciaId.ShouldBe(transferenciaId);
        enviada.IbanDestino.ShouldBe(destino.Iban);
        enviada.SaldoApos.ShouldBe(Dinheiro.De(70m));

        var recebida = destino.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecebida>();
        recebida.TransferenciaId.ShouldBe(transferenciaId);
        recebida.IbanOrigem.ShouldBe(origem.Iban);
        recebida.SaldoApos.ShouldBe(Dinheiro.De(30m));
    }

    [Fact]
    public void Transferir_ParaAMesmaConta_LancaExcecao()
    {
        var conta = NovaConta(1, 100m);

        Should.Throw<ExcecaoTransferenciaParaMesmaConta>(() => _servico.Transferir(conta, conta, Dinheiro.De(10m), Agora));
    }

    [Fact]
    public void Transferir_ComSaldoInsuficiente_LancaExcecaoENenhumaContaMuda()
    {
        var origem = NovaConta(1, 10m);
        var destino = NovaConta(2, 0m);

        Should.Throw<ExcecaoSaldoInsuficiente>(() => _servico.Transferir(origem, destino, Dinheiro.De(30m), Agora));

        DeveEstarIntacta(origem, 10m, 2);
        DeveEstarIntacta(destino, 0m, 1);
    }

    [Fact]
    public void Transferir_ParaDestinoEncerrado_LancaExcecaoENenhumaContaMuda()
    {
        var origem = NovaConta(1, 100m);
        var destino = NovaConta(2, 0m);
        destino.Encerrar(Agora);
        destino.LimparEventos();

        Should.Throw<ExcecaoContaEncerrada>(() => _servico.Transferir(origem, destino, Dinheiro.De(30m), Agora));

        DeveEstarIntacta(origem, 100m, 2);
    }

    [Fact]
    public void Transferir_DeOrigemBloqueada_LancaExcecaoContaBloqueada()
    {
        var origem = NovaConta(1, 100m);
        origem.Bloquear(Agora);
        var destino = NovaConta(2, 0m);

        Should.Throw<ExcecaoContaBloqueada>(() => _servico.Transferir(origem, destino, Dinheiro.De(30m), Agora));
    }

    [Fact]
    public void Transferir_ParaDestinoBloqueado_Funciona()
    {
        var origem = NovaConta(1, 100m);
        var destino = NovaConta(2, 0m);
        destino.Bloquear(Agora);

        _servico.Transferir(origem, destino, Dinheiro.De(30m), Agora);

        destino.Saldo.ShouldBe(Dinheiro.De(30m));
    }

    [Fact]
    public void Transferir_ValorZero_LancaExcecaoValorInvalido()
    {
        var origem = NovaConta(1, 100m);
        var destino = NovaConta(2, 0m);

        Should.Throw<ExcecaoValorInvalido>(
            () => _servico.Transferir(origem, destino, Dinheiro.Zero, Agora));
    }

    private static void DeveEstarIntacta(Conta conta, decimal saldo, int versao)
    {
        conta.Saldo.ShouldBe(Dinheiro.De(saldo));
        conta.Versao.ShouldBe(versao);
        conta.EventosDominio.ShouldBeEmpty();
    }
}
