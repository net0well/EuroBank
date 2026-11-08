using EuroBank.Extrato.Application.Handlers;
using EuroBank.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Wolverine;

namespace EuroBank.Extrato.Infrastructure.Messaging;

public static class ConfiguracaoMensageria
{
    public const string TopicoEventosContas = "eurobank.contas.eventos";

    /// <summary>
    /// Wolverine no Extrato: consome eurobank.contas.eventos com inbox durável no Postgres, retry para falhas
    /// transitórias e dead letter para o que não tem como dar certo.
    /// </summary>
    public static WolverineOptions ConfigurarMensageriaExtrato(this WolverineOptions opcoes, IConfiguration configuracao)
    {
        var kafka = configuracao.GetSection(OpcoesKafka.Secao).Get<OpcoesKafka>() ?? new OpcoesKafka();
        if (string.IsNullOrWhiteSpace(kafka.GrupoConsumidor))
        {
            kafka.GrupoConsumidor = "eurobank-extrato";
        }

        var connectionString = configuracao.GetConnectionString("Extrato")
            ?? throw new InvalidOperationException("ConnectionStrings:Extrato não configurada.");

        opcoes.UsarConvencoesEuroBank(typeof(ContaAbertaManipulador).Assembly);
        opcoes.UsarInboxDuravel(connectionString);
        opcoes.UsarKafka(kafka);
        opcoes.OuvirTopicoDebezium(TopicoEventosContas, TiposEventosContas.Mapa);
        opcoes.AplicarPoliticaDeErros();

        return opcoes;
    }
}
