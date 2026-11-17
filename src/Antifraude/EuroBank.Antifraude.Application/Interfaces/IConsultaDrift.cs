using EuroBank.Antifraude.Application.TransferObjects;

namespace EuroBank.Antifraude.Application.Interfaces;

/// <summary>O que o drift lê: a distribuição do treino da versão ativa e as análises da janela.</summary>
public interface IConsultaDrift
{
    /// <summary>A versão ativa e a distribuição do treino dela; nula sem versão ativa.</summary>
    Task<ReferenciaDrift?> ReferenciaAtivaAsync(CancellationToken cancellationToken);

    /// <summary>
    /// As features das análises criadas na janela (as mais recentes, até o máximo) e a probabilidade das que a versão
    /// pontuou.
    /// </summary>
    Task<AmostraDrift> AmostraAsync(int versao, DateTimeOffset inicio, DateTimeOffset fim, int maximo, CancellationToken cancellationToken);
}
