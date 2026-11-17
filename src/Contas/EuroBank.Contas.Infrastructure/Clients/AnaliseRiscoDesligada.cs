using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Application.TransferObjects;

namespace EuroBank.Contas.Infrastructure.Clients;

/// <summary>Antifraude desligado (Antifraude:Ativo = false, o padrão fora do compose): toda operação segue.</summary>
public sealed class AnaliseRiscoDesligada : IAnaliseRisco
{
    public Task<ResultadoRisco> AvaliarAsync(OperacaoEmAnalise operacao, CancellationToken cancellationToken) =>
        Task.FromResult(ResultadoRisco.SemAnalise);
}
