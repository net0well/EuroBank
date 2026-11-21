using EuroBank.Web.Antifraude.Handlers;
using EuroBank.Web.Antifraude.Services;
using EuroBank.Web.Antifraude.TransferObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace EuroBank.Web.Tests.Antifraude.Handlers;

/// <summary>Só o que mexe na fila vira aviso: caso aberto e decisões do analista, da expiração ou do cliente.</summary>
public sealed class EventosAntifraudeManipuladorTestes
{
    private readonly AvisosFila _avisos = new(NullLogger<AvisosFila>.Instance);
    private readonly List<AvisoFila> _recebidos = [];

    public EventosAntifraudeManipuladorTestes() => _avisos.Assinar(_recebidos.Add);

    private EventosAntifraudeManipulador Manipulador => new(_avisos);

    [Fact]
    public void CasoAberto_AvisoDeCasoNovo()
    {
        var evento = new EventoCasoAberto(Guid.CreateVersion7(), Guid.CreateVersion7(), 50, DateTimeOffset.UtcNow.AddMinutes(30), DateTimeOffset.UtcNow);

        Manipulador.Handle(evento);

        var aviso = _recebidos.ShouldHaveSingleItem();
        aviso.CasoNovo.ShouldBeTrue();
        aviso.CasoId.ShouldBe(evento.CasoId);
        aviso.OperacaoId.ShouldBe(evento.OperacaoId);
        aviso.ScoreFinal.ShouldBe(50);
    }

    [Theory]
    [InlineData("Analista", "Recusada")]
    [InlineData("Expiracao", "Expirada")]
    [InlineData("Cliente", "Cancelada")]
    public void DecisaoQueFechaUmCaso_Aviso(string origem, string decisao)
    {
        Manipulador.Handle(new EventoAnaliseDecidida(Guid.CreateVersion7(), decisao, origem, 50, origem == "Analista" ? "Marta" : null, DateTimeOffset.UtcNow));

        var aviso = _recebidos.ShouldHaveSingleItem();
        aviso.CasoNovo.ShouldBeFalse();
        aviso.Decisao.ShouldBe(decisao);
        aviso.Origem.ShouldBe(origem);
    }

    [Fact]
    public void DecisaoDoMotor_SemAviso()
    {
        Manipulador.Handle(new EventoAnaliseDecidida(Guid.CreateVersion7(), "Aprovada", "Motor", 10, null, DateTimeOffset.UtcNow));

        _recebidos.ShouldBeEmpty();
    }
}
