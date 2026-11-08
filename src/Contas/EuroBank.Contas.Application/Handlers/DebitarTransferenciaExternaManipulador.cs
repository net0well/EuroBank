using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Contas.Application.Handlers;

public sealed class DebitarTransferenciaExternaManipulador(
    IRepositorioContas repositorio,
    IRespostasSaga respostas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<DebitarTransferenciaExternaManipulador> logger)
{
    public async Task Handle(DebitarTransferenciaExterna comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterPorIdAsync(new ContaId(comando.ContaId), cancellationToken);
        if (conta is null)
        {
            respostas.DebitoRecusado(comando.TransferenciaId, comando.ContaId, MotivoRecusaMapeador.ContaInexistente);
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
            return;
        }

        try
        {
            var resultado = conta.DebitarTransferenciaExterna(
                comando.TransferenciaId,
                Dinheiro.De(comando.Valor),
                Dinheiro.De(comando.Tarifa),
                comando.IbanDestino,
                comando.NomeBeneficiario,
                comando.Descricao,
                relogio.GetUtcNow());

            // Mesmo num reenvio (JaProcessada), responde de novo com os valores originais.
            var transferencia = conta.Transferencias.First(t => t.Id == comando.TransferenciaId);
            respostas.DebitoRealizado(comando.TransferenciaId, comando.ContaId, transferencia.Valor, transferencia.Tarifa);
            logger.LogInformation(
                "Débito da transferência {TransferenciaId} na conta {ContaId}: {Resultado}",
                comando.TransferenciaId, comando.ContaId, resultado);
        }
        catch (ExcecaoDominio excecao) when (excecao is ExcecaoRegraNegocio or ExcecaoValidacao)
        {
            var motivo = MotivoRecusaMapeador.DoCodigo(excecao.Codigo);
            respostas.DebitoRecusado(comando.TransferenciaId, comando.ContaId, motivo);
            logger.LogInformation(
                "Débito da transferência {TransferenciaId} na conta {ContaId} recusado: {Motivo}",
                comando.TransferenciaId, comando.ContaId, motivo);
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
