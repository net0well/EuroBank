using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class ContaAbertaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<ContaAbertaManipulador> logger)
{
    public async Task Handle(EventoContaAberta evento, CancellationToken cancellationToken)
    {
        if (await repositorio.ObterAsync(evento.ContaId, cancellationToken) is not null)
        {
            logger.LogInformation("ContaAberta da conta {ContaId} ignorado: o extrato já existe", evento.ContaId);
            return;
        }

        repositorio.Adicionar(
            ExtratoConta.Abrir(
                evento.ContaId,
                evento.Iban,
                evento.Titular,
                evento.Versao,
                evento.OcorridoEm,
                string.IsNullOrWhiteSpace(evento.Moeda) ? Moeda.EUR : Moedas.Ler(evento.Moeda)));
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);

        logger.LogInformation("Extrato criado para a conta {ContaId} ({Iban})", evento.ContaId, evento.Iban);
    }
}
