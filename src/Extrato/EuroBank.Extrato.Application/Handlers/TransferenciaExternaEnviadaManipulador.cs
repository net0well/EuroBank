using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Extrato.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
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
            Dinheiro.De(evento.Valor, extrato.Moeda),
            Dinheiro.De(evento.Tarifa, extrato.Moeda),
            Saldo.De(evento.SaldoApos, extrato.Moeda),
            evento.OcorridoEm,
            evento is { MoedaDestino: { } moeda, ValorDestino: { } valor, Cotacao: { } cotacao }
                ? new CambioDoLancamento(Moedas.Ler(moeda), valor, cotacao)
                : null);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "TransferenciaExternaEnviada", evento.ContaId, evento.Versao);
    }
}
