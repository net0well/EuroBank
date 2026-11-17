using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Worker.Tests.Fixtures;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Worker.Tests.Perfil;

/// <summary>
/// O perfil aprende dispositivo e localização com as análises aprovadas que chegam pelo tópico do antifraude: só as
/// aprovadas, sem repetir a mesma análise, e as localizações fora da retenção saem.
/// </summary>
public sealed class UsoContaTestes(AntifraudeWorkerFixture worker)
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 17, 15, 0, 0, TimeSpan.Zero);
    private static readonly Localizacao Paris = new(48.8566, 2.3522, "FR");

    private static EventoAnaliseDecidida Decidida(Guid conta, string decisao, DateTimeOffset momento, string? dispositivo = "celular-da-ana", Localizacao? localizacao = null) =>
        new(Guid.CreateVersion7(), conta, decisao, momento, dispositivo, localizacao ?? Paris);

    [Fact]
    public async Task Aprovada_EnsinaDispositivoELocalizacao_SemRepetir()
    {
        var conta = Guid.CreateVersion7();
        var aprovada = Decidida(conta, "Aprovada", Agora.AddHours(-1));

        await worker.EntregarAsync(aprovada);
        await worker.EntregarAsync(aprovada);

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.DispositivosConhecidos.ShouldBe(["celular-da-ana"]);
        var localizacao = perfil.Localizacoes.ShouldHaveSingleItem();
        localizacao.Localizacao.ShouldBe(Paris);
        localizacao.Momento.ShouldBe(Agora.AddHours(-1));
    }

    [Theory]
    [InlineData("Recusada")]
    [InlineData("Expirada")]
    [InlineData("Cancelada")]
    public async Task NaoAprovada_NaoEnsina(string decisao)
    {
        var conta = Guid.CreateVersion7();

        await worker.EntregarAsync(Decidida(conta, decisao, Agora.AddHours(-1)));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.DispositivosConhecidos.ShouldBeEmpty();
        perfil.Localizacoes.ShouldBeEmpty();
    }

    [Fact]
    public async Task LocalizacaoForaDaRetencao_Sai()
    {
        var conta = Guid.CreateVersion7();
        var londres = new Localizacao(51.5074, -0.1278, "GB");

        await worker.EntregarAsync(Decidida(conta, "Aprovada", Agora.AddDays(-40), localizacao: londres));
        await worker.EntregarAsync(Decidida(conta, "Aprovada", Agora.AddHours(-2), dispositivo: null));

        var perfil = await worker.PerfilAsync(conta, Agora);
        perfil.Localizacoes.ShouldHaveSingleItem().Localizacao.ShouldBe(Paris);
        perfil.DispositivosConhecidos.ShouldBe(["celular-da-ana"]);
    }
}
