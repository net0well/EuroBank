using EuroBank.Antifraude.Domain.Exceptions;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Limiares do score e peso do modelo de ML. Cada mudança cria uma versão nova (a análise guarda a versão usada);
/// as antigas ficam para auditoria. Abaixo de LimiarRevisao aprova; a partir de LimiarRecusa recusa; entre os dois,
/// revisão humana.
/// </summary>
public sealed class ConfiguracaoDecisao
{
    private ConfiguracaoDecisao()
    {
        // Usado pelo EF Core.
    }

    public int Versao { get; private set; }

    public int LimiarRevisao { get; private set; }

    public int LimiarRecusa { get; private set; }

    /// <summary>Quanto o score do modelo (0 a 1) vale em pontos; 0 enquanto não há modelo.</summary>
    public int PesoModelo { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    public static ConfiguracaoDecisao Inicial(DateTimeOffset agora) => Criar(1, 30, 70, 0, agora);

    public ConfiguracaoDecisao NovaVersao(int limiarRevisao, int limiarRecusa, int pesoModelo, DateTimeOffset agora) =>
        Criar(Versao + 1, limiarRevisao, limiarRecusa, pesoModelo, agora);

    private static ConfiguracaoDecisao Criar(int versao, int limiarRevisao, int limiarRecusa, int pesoModelo, DateTimeOffset agora)
    {
        if (limiarRevisao is < 1 or > 100 || limiarRecusa is < 1 or > 100)
        {
            throw new ExcecaoConfiguracaoInvalida("os limiares devem estar entre 1 e 100");
        }

        if (limiarRevisao >= limiarRecusa)
        {
            throw new ExcecaoConfiguracaoInvalida("o limiar de revisão deve ser menor que o de recusa");
        }

        if (pesoModelo is < 0 or > 100)
        {
            throw new ExcecaoConfiguracaoInvalida("o peso do modelo deve estar entre 0 e 100");
        }

        return new ConfiguracaoDecisao
        {
            Versao = versao,
            LimiarRevisao = limiarRevisao,
            LimiarRecusa = limiarRecusa,
            PesoModelo = pesoModelo,
            CriadaEm = agora,
        };
    }
}
