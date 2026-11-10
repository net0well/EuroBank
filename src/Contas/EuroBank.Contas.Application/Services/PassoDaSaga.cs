using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Contas.Domain.Enums;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace EuroBank.Contas.Application.Services;

/// <summary>
/// Caminho comum dos comandos de débito e crédito da saga: carrega a conta, confere a moeda, executa a operação e
/// responde ao orquestrador pela outbox, na mesma transação. Regra de negócio violada não é erro do worker: vira
/// uma resposta de recusa. Repetir o comando (reenvio) responde de novo com os valores originais.
/// </summary>
public sealed class PassoDaSaga(
    IRepositorioContas repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<PassoDaSaga> logger)
{
    public async Task ExecutarAsync(
        Guid transferenciaId,
        Guid contaId,
        string? moedaEsperada,
        Func<Conta, ResultadoOperacao> operar,
        Action<TransferenciaDaConta> responderSucesso,
        Action<string> responderRecusa,
        CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterPorIdAsync(new ContaId(contaId), cancellationToken);
        if (conta is null)
        {
            responderRecusa(MotivoRecusaMapeador.ContaInexistente);
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
            return;
        }

        try
        {
            if (moedaEsperada is not null && Moedas.Ler(moedaEsperada) is var moeda && moeda != conta.Moeda)
            {
                throw new ExcecaoMoedaDiferente(conta.Moeda, moeda);
            }

            var resultado = operar(conta);
            responderSucesso(conta.Transferencias.First(transferencia => transferencia.Id == transferenciaId));
            logger.LogInformation(
                "Transferência {TransferenciaId} na conta {ContaId}: {Resultado}", transferenciaId, contaId, resultado);
        }
        catch (ExcecaoDominio excecao) when (excecao is ExcecaoRegraNegocio or ExcecaoValidacao)
        {
            var motivo = MotivoRecusaMapeador.DoCodigo(excecao.Codigo);
            responderRecusa(motivo);
            logger.LogInformation(
                "Transferência {TransferenciaId} na conta {ContaId} recusada: {Motivo}", transferenciaId, contaId, motivo);
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
