using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Mede o drift da versão ativa e grava a medição (sem versão ativa, não grava nada).</summary>
public sealed class MedirDriftManipulador(
    IConsultaDrift consulta,
    IRepositorioDrift medicoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<MedirDriftManipulador> logger)
{
    public async Task<ResultadoMedicaoDrift> Handle(MedirDrift comando, CancellationToken cancellationToken)
    {
        var apagadas = await medicoes.ApagarAnterioresAsync(comando.Agora - comando.Retencao, cancellationToken);
        if (await consulta.ReferenciaAtivaAsync(cancellationToken) is not { } referencia)
        {
            return new ResultadoMedicaoDrift(null);
        }

        var inicio = comando.Agora - comando.Janela;
        var amostra = await consulta.AmostraAsync(referencia.Versao, inicio, comando.Agora, comando.AmostraMaxima, cancellationToken);
        var medicao = MedicaoDrift.Medir(
            referencia.Versao, referencia.Distribuicao, amostra.Features, amostra.Scores, comando.Limites, inicio, comando.Agora);
        medicoes.Adicionar(medicao);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);

        logger.LogInformation(
            "Drift da versão {Versao}: {Situacao}, nível {Nivel}, PSI máximo {PsiMaximo} em {Amostra} análises ({Apagadas} medições antigas apagadas)",
            medicao.VersaoModelo,
            medicao.Situacao,
            medicao.Nivel,
            medicao.PsiMaximo,
            medicao.Amostra,
            apagadas);
        return new ResultadoMedicaoDrift(medicao.ParaDto());
    }
}
