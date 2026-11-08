using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class TransferenciaExternaEnviadaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<TransferenciaExternaEnviadaManipulador> logger)
{
    public async Task Handle(EventoTransferenciaExternaEnviada evento, CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(evento.ContaId, cancellationToken);

        var resultado = extrato.RegistrarTransferenciaExternaEnviada(
            evento.Versao,
            evento.TransferenciaId,
            evento.IbanDestino,
            evento.NomeBeneficiario,
            evento.Descricao,
            Dinheiro.De(evento.Valor),
            Dinheiro.De(evento.Tarifa),
            Dinheiro.De(evento.SaldoApos),
            evento.OcorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "TransferenciaExternaEnviada", evento.ContaId, evento.Versao);
    }
}
