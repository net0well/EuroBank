using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Grpc;
using Google.Protobuf.WellKnownTypes;

namespace EuroBank.Antifraude.Api.Tests.Fixtures;

/// <summary>Pedidos de avaliação e perfis de conta montados no Redis da API.</summary>
public sealed class CenarioAvaliacao(AntifraudeFixture antifraude)
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    public const string IbanConhecido = "DE89370400440532013000";

    public const string IbanBloqueado = "DE89370400440532013066";

    public const string IbanNovo = "FR1420041010050500013M02606";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static SolicitacaoAvaliacao Pedido(
        Guid conta,
        decimal valor = 100m,
        string iban = IbanConhecido,
        Guid? operacao = null,
        TipoOperacao tipo = TipoOperacao.Externa) =>
        new()
        {
            OperacaoId = (operacao ?? Guid.CreateVersion7()).ToString(),
            TipoOperacao = tipo,
            ContaId = conta.ToString(),
            Valor = valor,
            Moeda = "EUR",
            IbanDestino = iban,
            Momento = Timestamp.FromDateTimeOffset(Agora),
        };

    /// <summary>
    /// Conta aberta há 90 dias, saldo de 5.000 e 12 transferências de 100 às 15:00 UTC para o IBAN conhecido: uma
    /// externa comum de 100 não dispara nenhuma regra.
    /// </summary>
    public async Task<Guid> ContaComHistoricoAsync()
    {
        var conta = Guid.CreateVersion7();
        var abertaEm = Agora.AddDays(-90);
        await antifraude.Perfis.AplicarAsync(new AtualizacaoPerfil(conta, 1, abertaEm, SaldoApos: 0m, AbertaEm: abertaEm, Moeda: "EUR"), Ct);
        for (var dia = 12; dia >= 1; dia--)
        {
            await antifraude.Perfis.AplicarAsync(
                new AtualizacaoPerfil(conta, 14 - dia, Agora.AddDays(-dia), SaldoApos: 5_000m + 100m * dia, Saida: new SaidaPerfil(Guid.CreateVersion7(), 100m, IbanConhecido)),
                Ct);
        }

        await antifraude.Perfis.AplicarAsync(new AtualizacaoPerfil(conta, 14, Agora.AddHours(-1), SaldoApos: 5_000m), Ct);
        return conta;
    }
}
