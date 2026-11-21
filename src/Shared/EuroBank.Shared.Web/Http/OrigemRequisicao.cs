using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace EuroBank.Shared.Web.Http;

/// <summary>
/// De onde veio o pedido, pelos headers opcionais X-Dispositivo-Id (um id estável do aparelho, como o aplicativo o
/// gera) e X-Localizacao ("latitude,longitude,país"). Ausentes: nulos. Fora do formato: 400.
/// </summary>
public sealed record OrigemRequisicao(string? DispositivoId, Localizacao? Localizacao)
{
    public const string CabecalhoDispositivo = "X-Dispositivo-Id";
    public const string CabecalhoLocalizacao = "X-Localizacao";

    public const int TamanhoMaximoDispositivo = 100;

    public static OrigemRequisicao Nenhuma { get; } = new(null, null);

    /// <exception cref="ExcecaoDispositivoInvalido">Id do dispositivo longo demais.</exception>
    /// <exception cref="ExcecaoLocalizacaoInvalida">Localização fora do formato.</exception>
    public static OrigemRequisicao Ler(HttpRequest requisicao)
    {
        ArgumentNullException.ThrowIfNull(requisicao);
        var dispositivo = requisicao.Headers[CabecalhoDispositivo].ToString().Trim();
        if (dispositivo.Length > TamanhoMaximoDispositivo)
        {
            throw new ExcecaoDispositivoInvalido();
        }

        var localizacao = requisicao.Headers[CabecalhoLocalizacao].ToString().Trim();
        return new OrigemRequisicao(
            dispositivo.Length == 0 ? null : dispositivo,
            localizacao.Length == 0 ? null : Localizacao.Ler(localizacao));
    }
}
