using EuroBank.Contas.Dominio.Comum;
using EuroBank.Contas.Dominio.Contas;
using EuroBank.Contas.Dominio.Contas.Eventos;

namespace EuroBank.Contas.Dominio.Testes;

public sealed class ContaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private static Conta NovaConta(decimal saldoInicial = 0m)
    {
        var conta = Conta.Abrir(Iban.Gerar(1), Titular.Criar("Ana"), Agora);
        if (saldoInicial > 0)
        {
            conta.Depositar(Dinheiro.De(saldoInicial), Agora);
        }

        conta.LimparEventos();
        return conta;
    }

    [Fact]
    public void Abrir_CriaContaAtivaComSaldoZeroVersaoUmEEventoContaAberta()
    {
        var iban = Iban.Gerar(1);

        var conta = Conta.Abrir(iban, Titular.Criar("Ana"), Agora);

        conta.Id.Valor.Version.ShouldBe(7);
        conta.Iban.ShouldBe(iban);
        conta.Titular.Nome.ShouldBe("Ana");
        conta.Saldo.ShouldBe(Dinheiro.Zero);
        conta.Status.ShouldBe(StatusConta.Ativa);
        conta.Versao.ShouldBe(1);
        conta.AbertaEm.ShouldBe(Agora);
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ContaAberta>();
        evento.ContaId.ShouldBe(conta.Id);
        evento.Iban.ShouldBe(iban);
        evento.Versao.ShouldBe(1);
    }

    [Fact]
    public void Depositar_EmContaAtiva_AumentaSaldoIncrementaVersaoERegistraEvento()
    {
        var conta = NovaConta();

        conta.Depositar(Dinheiro.De(100m), Agora);

        conta.Saldo.ShouldBe(Dinheiro.De(100m));
        conta.Versao.ShouldBe(2);
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<DepositoRealizado>();
        evento.Valor.ShouldBe(Dinheiro.De(100m));
        evento.SaldoApos.ShouldBe(Dinheiro.De(100m));
        evento.Versao.ShouldBe(2);
    }

    [Fact]
    public void Depositar_ValorZero_LancaExcecaoValorInvalidoSemAlterarConta()
    {
        var conta = NovaConta();

        Should.Throw<ExcecaoValorInvalido>(() => conta.Depositar(Dinheiro.Zero, Agora));

        conta.Versao.ShouldBe(1);
        conta.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Sacar_ComSaldoSuficiente_ReduzSaldoERegistraEvento()
    {
        var conta = NovaConta(100m);

        conta.Sacar(Dinheiro.De(40m), Agora);

        conta.Saldo.ShouldBe(Dinheiro.De(60m));
        var evento = conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<SaqueRealizado>();
        evento.Valor.ShouldBe(Dinheiro.De(40m));
        evento.SaldoApos.ShouldBe(Dinheiro.De(60m));
        evento.Versao.ShouldBe(3);
    }

    [Fact]
    public void Sacar_ComSaldoInsuficiente_LancaExcecaoESaldoNaoMuda()
    {
        var conta = NovaConta(50m);

        Should.Throw<ExcecaoSaldoInsuficiente>(() => conta.Sacar(Dinheiro.De(80m), Agora));

        conta.Saldo.ShouldBe(Dinheiro.De(50m));
        conta.Versao.ShouldBe(2);
        conta.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Sacar_ValorZero_LancaExcecaoValorInvalido()
    {
        var conta = NovaConta(50m);

        Should.Throw<ExcecaoValorInvalido>(() => conta.Sacar(Dinheiro.Zero, Agora));
    }

    [Fact]
    public void Bloquear_ContaAtiva_FicaBloqueadaERegistraEvento()
    {
        var conta = NovaConta();

        conta.Bloquear(Agora);

        conta.Status.ShouldBe(StatusConta.Bloqueada);
        conta.Versao.ShouldBe(2);
        conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ContaBloqueada>().Versao.ShouldBe(2);
    }

    [Fact]
    public void ContaBloqueada_RejeitaSaqueEBloqueioMasAceitaDeposito()
    {
        var conta = NovaConta(100m);
        conta.Bloquear(Agora);

        Should.Throw<ExcecaoContaBloqueada>(() => conta.Sacar(Dinheiro.De(10m), Agora));
        Should.Throw<ExcecaoContaJaBloqueada>(() => conta.Bloquear(Agora));
        conta.Depositar(Dinheiro.De(10m), Agora);

        conta.Saldo.ShouldBe(Dinheiro.De(110m));
    }

    [Fact]
    public void Desbloquear_ContaBloqueada_VoltaParaAtivaERegistraEvento()
    {
        var conta = NovaConta();
        conta.Bloquear(Agora);
        conta.LimparEventos();

        conta.Desbloquear(Agora);

        conta.Status.ShouldBe(StatusConta.Ativa);
        conta.Versao.ShouldBe(3);
        conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ContaDesbloqueada>();
    }

    [Fact]
    public void Desbloquear_ContaNaoBloqueada_LancaExcecaoContaNaoBloqueada()
    {
        var conta = NovaConta();

        Should.Throw<ExcecaoContaNaoBloqueada>(() => conta.Desbloquear(Agora));
    }

    [Fact]
    public void Encerrar_ComSaldoZero_FicaEncerradaERegistraEvento()
    {
        var conta = NovaConta();

        conta.Encerrar(Agora);

        conta.Status.ShouldBe(StatusConta.Encerrada);
        conta.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<ContaEncerrada>();
    }

    [Fact]
    public void Encerrar_ContaBloqueadaComSaldoZero_FicaEncerrada()
    {
        var conta = NovaConta();
        conta.Bloquear(Agora);

        conta.Encerrar(Agora);

        conta.Status.ShouldBe(StatusConta.Encerrada);
    }

    [Fact]
    public void Encerrar_ComSaldo_LancaExcecaoContaComSaldo()
    {
        var conta = NovaConta(1m);

        Should.Throw<ExcecaoContaComSaldo>(() => conta.Encerrar(Agora));

        conta.Status.ShouldBe(StatusConta.Ativa);
    }

    [Fact]
    public void ContaEncerrada_RejeitaQualquerOperacao()
    {
        var conta = NovaConta();
        conta.Encerrar(Agora);

        Should.Throw<ExcecaoContaEncerrada>(() => conta.Depositar(Dinheiro.De(1m), Agora));
        Should.Throw<ExcecaoContaEncerrada>(() => conta.Sacar(Dinheiro.De(1m), Agora));
        Should.Throw<ExcecaoContaEncerrada>(() => conta.Bloquear(Agora));
        Should.Throw<ExcecaoContaEncerrada>(() => conta.Desbloquear(Agora));
        Should.Throw<ExcecaoContaEncerrada>(() => conta.Encerrar(Agora));
    }
}
