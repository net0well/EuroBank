using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

/// <summary>Eventos de status só mudam o status do extrato; não geram lançamento.</summary>
public sealed class StatusContaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<StatusContaManipulador> logger)
{
    public Task Handle(EventoContaBloqueada evento, CancellationToken cancellationToken) =>
        AlterarAsync(evento.ContaId, StatusConta.Bloqueada, evento.Versao, evento.OcorridoEm, "ContaBloqueada", cancellationToken);

    public Task Handle(EventoContaDesbloqueada evento, CancellationToken cancellationToken) =>
        AlterarAsync(evento.ContaId, StatusConta.Ativa, evento.Versao, evento.OcorridoEm, "ContaDesbloqueada", cancellationToken);

    public Task Handle(EventoContaEncerrada evento, CancellationToken cancellationToken) =>
        AlterarAsync(evento.ContaId, StatusConta.Encerrada, evento.Versao, evento.OcorridoEm, "ContaEncerrada", cancellationToken);

    private async Task AlterarAsync(
        Guid contaId,
        StatusConta status,
        int versao,
        DateTimeOffset ocorridoEm,
        string tipoEvento,
        CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(contaId, cancellationToken);

        var resultado = extrato.AlterarStatus(status, versao, ocorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, tipoEvento, contaId, versao);
    }
}
