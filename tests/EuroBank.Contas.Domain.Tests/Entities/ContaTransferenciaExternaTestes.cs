using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.Tests.Entities;

public sealed class ContaTransferenciaExternaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly Dinheiro Tarifa = Dinheiro.De(2.50m);

    private static Conta NovaConta(decimal saldo)
    {
        var conta = Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora);
        if (saldo > 0)
        {
            conta.Depositar(Dinheiro.De(saldo), Agora);
        }

        conta.LimparEventos();
        return conta;
    }

    private static ResultadoOperacao Debitar(Conta conta, Guid transferenciaId, decimal valor) =>
        conta.DebitarTransferenciaExterna(
            transferenciaId, Dinheiro.De(valor), Tarifa, "DE89370400440532013000", "Bruno Silva", "Aluguel", Agora);

    [Fact]
    public void Debitar_DebitaValorMaisTarifa_RegistraTravaEEvento()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();

        var resultado = Debitar(conta, transferenciaId, 100m);

        resultado.ShouldBe(ResultadoOperacao.Aplicada);
        conta.Saldo.ShouldBe(Dinheiro.De(897.50m));
        conta.Versao.ShouldBe(3);
        conta.TemTransferenciaEmAndamento.ShouldBeTrue();
        var trava = conta.Transferencias.ShouldHaveSingleItem();
        trava.Id.ShouldBe(transferenciaId);
        trava.Status.ShouldBe(StatusTransferenciaDaConta.Pendente);
        trava.Valor.ShouldBe(Dinheiro.De(100m));
        trava.Tarifa.ShouldBe(Tarifa);

        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaExternaEnviada>();
        evento.TransferenciaId.ShouldBe(transferenciaId);
        evento.Valor.ShouldBe(Dinheiro.De(100m));
        evento.Tarifa.ShouldBe(Tarifa);
        evento.SaldoApos.ShouldBe(Dinheiro.De(897.50m));
        evento.NomeBeneficiario.ShouldBe("Bruno Silva");
        evento.Versao.ShouldBe(3);
    }

    [Fact]
    public void Debitar_MesmoIdDuasVezes_NaoDebitaDeNovo()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();
        Debitar(conta, transferenciaId, 100m);
        conta.LimparEventos();

        var repetido = Debitar(conta, transferenciaId, 100m);

        repetido.ShouldBe(ResultadoOperacao.JaProcessada);
        conta.Saldo.ShouldBe(Dinheiro.De(897.50m));
        conta.Versao.ShouldBe(3);
        conta.EventosDominio.ShouldBeEmpty();
        conta.Transferencias.Count.ShouldBe(1);
    }

    [Fact]
    public void Debitar_SemSaldoParaValorMaisTarifa_LancaSaldoInsuficienteSemTrava()
    {
        var conta = NovaConta(100m);

        Should.Throw<ExcecaoSaldoInsuficiente>(() => Debitar(conta, Guid.CreateVersion7(), 99m));

        conta.Saldo.ShouldBe(Dinheiro.De(100m));
        conta.Transferencias.ShouldBeEmpty();
    }

    [Fact]
    public void Debitar_ContaBloqueada_LancaContaBloqueada()
    {
        var conta = NovaConta(1000m);
        conta.Bloquear(Agora);

        Should.Throw<ExcecaoContaBloqueada>(() => Debitar(conta, Guid.CreateVersion7(), 10m));
    }

    [Fact]
    public void Debitar_ValorZero_LancaValorInvalido()
    {
        var conta = NovaConta(1000m);

        Should.Throw<ExcecaoValorInvalido>(() => Debitar(conta, Guid.CreateVersion7(), 0m));
    }

    [Fact]
    public void Estornar_DevolveValorMaisTarifa_LiberaATravaERegistraEvento()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();
        Debitar(conta, transferenciaId, 100m);
        conta.LimparEventos();

        var resultado = conta.EstornarTransferencia(transferenciaId, Agora);

        resultado.ShouldBe(ResultadoOperacao.Aplicada);
        conta.Saldo.ShouldBe(Dinheiro.De(1000m));
        conta.Versao.ShouldBe(4);
        conta.TemTransferenciaEmAndamento.ShouldBeFalse();
        conta.Transferencias.ShouldHaveSingleItem().Status.ShouldBe(StatusTransferenciaDaConta.Estornada);
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<EstornoTransferencia>();
        evento.Valor.ShouldBe(Dinheiro.De(102.50m));
        evento.NomeBeneficiario.ShouldBe("Bruno Silva");
        evento.SaldoApos.ShouldBe(Dinheiro.De(1000m));
    }

    [Fact]
    public void Estornar_DuasVezes_DevolveUmaVezSo()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();
        Debitar(conta, transferenciaId, 100m);
        conta.EstornarTransferencia(transferenciaId, Agora);
        conta.LimparEventos();

        conta.EstornarTransferencia(transferenciaId, Agora).ShouldBe(ResultadoOperacao.JaProcessada);

        conta.Saldo.ShouldBe(Dinheiro.De(1000m));
        conta.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Estornar_EmContaBloqueada_FuncionaPorqueECredito()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();
        Debitar(conta, transferenciaId, 100m);
        conta.Bloquear(Agora);

        conta.EstornarTransferencia(transferenciaId, Agora);

        conta.Saldo.ShouldBe(Dinheiro.De(1000m));
    }

    [Fact]
    public void Estornar_TransferenciaDesconhecida_Lanca()
    {
        var conta = NovaConta(1000m);

        Should.Throw<ExcecaoTransferenciaDesconhecida>(() => conta.EstornarTransferencia(Guid.CreateVersion7(), Agora));
    }

    [Fact]
    public void LiberarTrava_ConcluiSemMexerNoSaldoNemNaVersao_EDepoisNaoPermiteEstorno()
    {
        var conta = NovaConta(1000m);
        var transferenciaId = Guid.CreateVersion7();
        Debitar(conta, transferenciaId, 100m);
        conta.LimparEventos();

        conta.LiberarTrava(transferenciaId).ShouldBe(ResultadoOperacao.Aplicada);
        conta.LiberarTrava(transferenciaId).ShouldBe(ResultadoOperacao.JaProcessada);

        conta.Saldo.ShouldBe(Dinheiro.De(897.50m));
        conta.Versao.ShouldBe(3);
        conta.EventosDominio.ShouldBeEmpty();
        conta.TemTransferenciaEmAndamento.ShouldBeFalse();
        Should.Throw<ExcecaoTransferenciaJaConcluida>(() => conta.EstornarTransferencia(transferenciaId, Agora));
    }

    [Fact]
    public void Encerrar_ComTransferenciaEmAndamento_LancaExcecao()
    {
        var conta = NovaConta(102.50m);
        Debitar(conta, Guid.CreateVersion7(), 100m);

        conta.Saldo.ShouldBe(Dinheiro.Zero);
        Should.Throw<ExcecaoContaComTransferenciaEmAndamento>(() => conta.Encerrar(Agora));
    }
}
