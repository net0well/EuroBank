using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Tests.Entities;

public sealed class ExtratoContaTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ContaId = Guid.CreateVersion7();

    private static ExtratoConta NovoExtrato() =>
        ExtratoConta.Abrir(ContaId, "PT50EURO0000000000001", "Ana", versao: 1, Agora);

    [Fact]
    public void Abrir_CriaExtratoAtivoComSaldoZeroEVersaoDoEvento()
    {
        var extrato = NovoExtrato();

        extrato.ContaId.ShouldBe(ContaId);
        extrato.Iban.ShouldBe("PT50EURO0000000000001");
        extrato.Titular.ShouldBe("Ana");
        extrato.Status.ShouldBe(StatusConta.Ativa);
        extrato.SaldoAtual.ShouldBe(Dinheiro.Zero);
        extrato.UltimaVersao.ShouldBe(1);
        extrato.Lancamentos.ShouldBeEmpty();
    }

    [Fact]
    public void RegistrarDeposito_CriaLancamentoEUsaOSaldoDoEvento()
    {
        var extrato = NovoExtrato();

        var resultado = extrato.RegistrarDeposito(2, Dinheiro.De(100m), Dinheiro.De(100m), Agora);

        resultado.ShouldBe(ResultadoAplicacao.Aplicado);
        extrato.SaldoAtual.ShouldBe(Dinheiro.De(100m));
        extrato.UltimaVersao.ShouldBe(2);
        var lancamento = extrato.Lancamentos.ShouldHaveSingleItem();
        lancamento.Tipo.ShouldBe(TipoLancamento.Deposito);
        lancamento.Valor.ShouldBe(Dinheiro.De(100m));
        lancamento.SaldoApos.ShouldBe(Dinheiro.De(100m));
        lancamento.Versao.ShouldBe(2);
        lancamento.Descricao.ShouldBe("Depósito");
    }

    [Fact]
    public void RegistrarSaqueETransferencias_CriamLancamentosComDescricao()
    {
        var extrato = NovoExtrato();
        var transferenciaId = Guid.CreateVersion7();

        extrato.RegistrarDeposito(2, Dinheiro.De(100m), Dinheiro.De(100m), Agora);
        extrato.RegistrarSaque(3, Dinheiro.De(10m), Dinheiro.De(90m), Agora);
        extrato.RegistrarTransferenciaEnviada(4, transferenciaId, "PT50EURO0000000000002", Dinheiro.De(30m), Dinheiro.De(60m), Agora);
        extrato.RegistrarTransferenciaRecebida(5, transferenciaId, "PT50EURO0000000000003", Dinheiro.De(5m), Dinheiro.De(65m), Agora);

        extrato.SaldoAtual.ShouldBe(Dinheiro.De(65m));
        extrato.Lancamentos.Select(l => l.Tipo).ShouldBe(
        [
            TipoLancamento.Deposito,
            TipoLancamento.Saque,
            TipoLancamento.TransferenciaEnviada,
            TipoLancamento.TransferenciaRecebida,
        ]);
        extrato.Lancamentos[2].Descricao.ShouldBe("Transferência para PT50EURO0000000000002");
        extrato.Lancamentos[3].Descricao.ShouldBe("Transferência de PT50EURO0000000000003");
    }

    [Fact]
    public void TransferenciaExternaEEstorno_CriamLancamentosComTarifaEDescricao()
    {
        var extrato = NovoExtrato();
        var transferenciaId = Guid.CreateVersion7();
        extrato.RegistrarDeposito(2, Dinheiro.De(1000m), Dinheiro.De(1000m), Agora);

        extrato.RegistrarTransferenciaExternaEnviada(
            3, transferenciaId, "DE89370400440532013000", "Bruno Silva", "Aluguel",
            Dinheiro.De(100m), Dinheiro.De(2.50m), Dinheiro.De(897.50m), Agora);
        extrato.RegistrarEstornoTransferencia(4, transferenciaId, "Bruno Silva", Dinheiro.De(102.50m), Dinheiro.De(1000m), Agora);

        extrato.SaldoAtual.ShouldBe(Dinheiro.De(1000m));
        var enviada = extrato.Lancamentos[1];
        enviada.Tipo.ShouldBe(TipoLancamento.TransferenciaExternaEnviada);
        enviada.Valor.ShouldBe(Dinheiro.De(100m));
        enviada.Tarifa.ShouldBe(Dinheiro.De(2.50m));
        enviada.TransferenciaId.ShouldBe(transferenciaId);
        enviada.Descricao.ShouldBe("Transferência para Bruno Silva (DE89370400440532013000): Aluguel");
        var estorno = extrato.Lancamentos[2];
        estorno.Tipo.ShouldBe(TipoLancamento.EstornoTransferencia);
        estorno.Valor.ShouldBe(Dinheiro.De(102.50m));
        estorno.Tarifa.ShouldBeNull();
        estorno.Descricao.ShouldBe("Estorno da transferência para Bruno Silva");
    }

    [Fact]
    public void EventoComVersaoJaAplicada_EIgnoradoESaldoNaoMuda()
    {
        var extrato = NovoExtrato();
        extrato.RegistrarDeposito(2, Dinheiro.De(100m), Dinheiro.De(100m), Agora);

        var repetido = extrato.RegistrarDeposito(2, Dinheiro.De(100m), Dinheiro.De(100m), Agora);
        var antigo = extrato.RegistrarSaque(1, Dinheiro.De(50m), Dinheiro.De(50m), Agora);

        repetido.ShouldBe(ResultadoAplicacao.IgnoradoJaAplicado);
        antigo.ShouldBe(ResultadoAplicacao.IgnoradoJaAplicado);
        extrato.SaldoAtual.ShouldBe(Dinheiro.De(100m));
        extrato.Lancamentos.Count.ShouldBe(1);
        extrato.UltimaVersao.ShouldBe(2);
    }

    [Fact]
    public void EventoComLacunaDeVersao_EAplicadoEIndicaALacuna()
    {
        var extrato = NovoExtrato();

        var resultado = extrato.RegistrarDeposito(4, Dinheiro.De(10m), Dinheiro.De(10m), Agora);

        resultado.ShouldBe(ResultadoAplicacao.AplicadoComLacuna);
        extrato.UltimaVersao.ShouldBe(4);
        extrato.SaldoAtual.ShouldBe(Dinheiro.De(10m));
    }

    [Theory]
    [InlineData(StatusConta.Bloqueada)]
    [InlineData(StatusConta.Ativa)]
    [InlineData(StatusConta.Encerrada)]
    public void AlterarStatus_AtualizaStatusSemCriarLancamento(StatusConta status)
    {
        var extrato = NovoExtrato();

        var resultado = extrato.AlterarStatus(status, 2, Agora);

        resultado.ShouldBe(ResultadoAplicacao.Aplicado);
        extrato.Status.ShouldBe(status);
        extrato.UltimaVersao.ShouldBe(2);
        extrato.Lancamentos.ShouldBeEmpty();
    }

    [Fact]
    public void AlterarStatus_ComVersaoJaAplicada_EIgnorado()
    {
        var extrato = NovoExtrato();
        extrato.AlterarStatus(StatusConta.Bloqueada, 2, Agora);

        var resultado = extrato.AlterarStatus(StatusConta.Ativa, 2, Agora);

        resultado.ShouldBe(ResultadoAplicacao.IgnoradoJaAplicado);
        extrato.Status.ShouldBe(StatusConta.Bloqueada);
    }
}
