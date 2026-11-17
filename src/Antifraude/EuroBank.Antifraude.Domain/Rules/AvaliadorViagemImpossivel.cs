using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>
/// Da última operação aprovada até esta, o cliente teria viajado mais rápido que um avião (900 km/h por padrão). Abaixo
/// da distância mínima não dispara: o GPS de duas operações seguidas varia alguns quilômetros.
/// </summary>
public sealed class AvaliadorViagemImpossivel : IAvaliadorRegra
{
    public CodigoRegra Codigo => CodigoRegra.ViagemImpossivel;

    public ResultadoRegra Avaliar(ContextoOperacao operacao, PerfilConta perfil, ListasRisco listas, Regra regra)
    {
        if (operacao.Localizacao is not { } atual)
        {
            return ResultadoRegra.Nao("Operação sem localização informada");
        }

        if (perfil.UltimaLocalizacao(operacao.Momento) is not { } anterior)
        {
            return ResultadoRegra.Nao("Sem localização anterior da conta");
        }

        var distancia = anterior.Localizacao.DistanciaKm(atual);
        var minutos = (operacao.Momento - anterior.Momento).TotalMinutes;
        if (distancia < (double)regra.Parametro("distanciaMinimaKm"))
        {
            return ResultadoRegra.Nao($"{Formatacao.Numero((decimal)Math.Round(distancia, 1))} km da operação anterior");
        }

        var velocidade = minutos <= 0 ? double.PositiveInfinity : distancia / (minutos / 60);
        var limite = (double)regra.Parametro("velocidadeMaximaKmH");
        var trajeto = $"{Formatacao.Numero((decimal)Math.Round(distancia))} km de {anterior.Localizacao.Pais} a {atual.Pais} em {Formatacao.Numero((decimal)Math.Round(minutos))} min";
        return velocidade > limite
            ? ResultadoRegra.Sim($"{trajeto}: mais rápido que {Formatacao.Numero((decimal)limite)} km/h")
            : ResultadoRegra.Nao($"{trajeto}: possível");
    }
}
