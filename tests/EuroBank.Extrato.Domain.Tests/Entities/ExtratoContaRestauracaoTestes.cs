using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Tests.Entities;

/// <summary>
/// O write-behind guarda o estado do extrato no Redis e o reconstitui a cada evento: o agregado restaurado aplica a
/// mesma regra de versão de um carregado do Postgres.
/// </summary>
public sealed class ExtratoContaRestauracaoTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid ContaId = Guid.CreateVersion7();

    private static ExtratoConta Restaurado() =>
        ExtratoConta.Restaurar(
            ContaId, "PT50EURO0000000000001", "Ana", StatusConta.Bloqueada, Moeda.USD, Saldo.De(250m, Moeda.USD), ultimaVersao: 7, Agora);

    [Fact]
    public void Restaurar_DevolveOEstadoGuardado_SemLancamentos()
    {
        var extrato = Restaurado();

        extrato.ContaId.ShouldBe(ContaId);
        extrato.Iban.ShouldBe("PT50EURO0000000000001");
        extrato.Titular.ShouldBe("Ana");
        extrato.Status.ShouldBe(StatusConta.Bloqueada);
        extrato.Moeda.ShouldBe(Moeda.USD);
        extrato.SaldoAtual.ShouldBe(Saldo.De(250m, Moeda.USD));
        extrato.UltimaVersao.ShouldBe(7);
        extrato.AtualizadoEm.ShouldBe(Agora);
        extrato.Lancamentos.ShouldBeEmpty();
    }

    [Fact]
    public void Restaurado_IgnoraVersaoJaAplicada_EAplicaANova()
    {
        var extrato = Restaurado();

        extrato.RegistrarDeposito(7, Dinheiro.De(10m, Moeda.USD), Saldo.De(260m, Moeda.USD), Agora).ShouldBe(ResultadoAplicacao.IgnoradoJaAplicado);
        extrato.RegistrarDeposito(8, Dinheiro.De(10m, Moeda.USD), Saldo.De(260m, Moeda.USD), Agora.AddMinutes(1)).ShouldBe(ResultadoAplicacao.Aplicado);

        extrato.UltimaVersao.ShouldBe(8);
        extrato.Lancamentos.ShouldHaveSingleItem().Versao.ShouldBe(8);
    }

    [Fact]
    public void Restaurar_ComSaldoEmOutraMoeda_Falha()
    {
        Should.Throw<ExcecaoMoedaDiferente>(() => ExtratoConta.Restaurar(
            ContaId, "PT50EURO0000000000001", "Ana", StatusConta.Ativa, Moeda.USD, Saldo.De(1m, Moeda.EUR), 1, Agora));
    }
}
