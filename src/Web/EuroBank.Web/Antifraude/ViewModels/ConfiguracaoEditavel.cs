using EuroBank.Web.Antifraude.TransferObjects;

namespace EuroBank.Web.Antifraude.ViewModels;

/// <summary>Os limiares e o peso do modelo sendo editados; salvar cria uma versão nova da configuração.</summary>
public sealed class ConfiguracaoEditavel
{
    public int LimiarRevisao { get; set; }

    public int LimiarRecusa { get; set; }

    public int PesoModelo { get; set; }

    public static ConfiguracaoEditavel De(ConfiguracaoDecisao configuracao) =>
        new() { LimiarRevisao = configuracao.LimiarRevisao, LimiarRecusa = configuracao.LimiarRecusa, PesoModelo = configuracao.PesoModelo };

    public ConfiguracaoRequisicao ParaRequisicao() => new(LimiarRevisao, LimiarRecusa, PesoModelo);
}
