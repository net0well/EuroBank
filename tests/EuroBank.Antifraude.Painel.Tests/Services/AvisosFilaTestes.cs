using EuroBank.Antifraude.Painel.Services;
using EuroBank.Antifraude.Painel.TransferObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace EuroBank.Antifraude.Painel.Tests.Services;

/// <summary>Os avisos chegam a todas as telas abertas; uma tela fechada (ou quebrada) não atrapalha as outras.</summary>
public sealed class AvisosFilaTestes
{
    private static readonly AvisoFila Aviso = new(Guid.CreateVersion7(), Guid.CreateVersion7(), 50, null, null, null);

    private readonly AvisosFila _avisos = new(NullLogger<AvisosFila>.Instance);

    [Fact]
    public void Publicar_ChegaATodas_AFechadaNaoRecebeMais()
    {
        var primeira = new List<AvisoFila>();
        var segunda = new List<AvisoFila>();
        var assinatura = _avisos.Assinar(primeira.Add);
        _avisos.Assinar(segunda.Add);

        _avisos.Publicar(Aviso);
        assinatura.Dispose();
        _avisos.Publicar(Aviso);

        primeira.Count.ShouldBe(1);
        segunda.Count.ShouldBe(2);
    }

    [Fact]
    public void UmaTelaQuebrada_AsOutrasRecebem()
    {
        var recebidos = new List<AvisoFila>();
        _avisos.Assinar(_ => throw new InvalidOperationException("circuito fechado"));
        _avisos.Assinar(recebidos.Add);

        _avisos.Publicar(Aviso);

        recebidos.ShouldHaveSingleItem();
    }
}
