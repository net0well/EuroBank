using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Domain.Tests;

/// <summary>Operações, perfis e configurações prontos para os testes do motor e dos agregados.</summary>
internal static class Cenarios
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    public static readonly Guid Conta = Guid.CreateVersion7();

    public const string IbanConhecido = "DE89370400440532013000";

    public const string IbanNovo = "FR1420041010050500013M02606";

    public static ContextoOperacao Operacao(
        decimal valor = 100m,
        string? iban = IbanConhecido,
        TipoOperacao tipo = TipoOperacao.Externa,
        DateTimeOffset? momento = null) =>
        new(Guid.CreateVersion7(), tipo, Conta, Dinheiro.De(valor), iban, momento ?? Agora);

    /// <summary>
    /// Conta aberta há 90 dias, com saldo de 5.000 e 12 transferências de 100 nos últimos 12 dias, sempre às 15:00 UTC
    /// (a hora de <see cref="Agora"/>) e para o IBAN conhecido. Nenhuma regra dispara para uma externa comum de 100.
    /// </summary>
    public static PerfilConta PerfilComHistorico(
        decimal saldo = 5_000m,
        IEnumerable<SaidaConta>? saidasExtras = null,
        DateTimeOffset? abertaEm = null)
    {
        var saidas = Enumerable.Range(1, 12)
            .Select(dia => new SaidaConta(Agora.AddDays(-dia), 100m, IbanConhecido))
            .Concat(saidasExtras ?? [])
            .ToList();
        return new PerfilConta(Conta, abertaEm ?? Agora.AddDays(-90), saldo, saidas, new HashSet<string> { IbanConhecido });
    }

    public static IReadOnlyList<Regra> Regras() => CatalogoRegras.RegrasIniciais(Agora);

    public static ConfiguracaoDecisao Configuracao() => ConfiguracaoDecisao.Inicial(Agora);

    public static ListasRisco SemListas() => ListasRisco.Vazias;

    public static ListasRisco Listas(IEnumerable<string>? bloqueados = null, IEnumerable<string>? paises = null) =>
        new(new HashSet<string>(bloqueados ?? []), new HashSet<string>(paises ?? []));
}
