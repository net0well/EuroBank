using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;

namespace EuroBank.Antifraude.Application.Mappers;

public static class DriftMapeador
{
    public static MedicaoDriftDto ParaDto(this MedicaoDrift medicao) =>
        new(
            medicao.VersaoModelo,
            medicao.MedidaEm,
            medicao.InicioJanela,
            medicao.Amostra,
            medicao.AmostraScore,
            medicao.Situacao.ToString(),
            medicao.Nivel?.ToString(),
            medicao.PsiMaximo is { } maximo ? Math.Round(maximo, 4) : null,
            medicao.Psis
                .OrderByDescending(psi => psi.Psi)
                .Select(psi => new PsiFeatureDto(psi.Nome, Math.Round(psi.Psi, 4), psi.Nivel.ToString()))
                .ToList());
}
