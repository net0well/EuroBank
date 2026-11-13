namespace EuroBank.Simuladores.BancoExterno.Regras;

/// <summary>Seção "BancoExterno" do appsettings.</summary>
public sealed class OpcoesBancoExterno
{
    public const string Secao = "BancoExterno";

    /// <summary>Tempo até responder uma liquidação ou consulta comum.</summary>
    public int LatenciaMs { get; set; } = 1000;

    /// <summary>Atraso da resposta para IBAN com final 97 (maior que o prazo da saga, para forçar o reenvio).</summary>
    public int AtrasoFinal97Segundos { get; set; } = 45;

    /// <summary>Final 95: a consulta diz NaoLiquidada, mas a confirmação chega depois deste atraso (Divergente).</summary>
    public int AtrasoFinal95Segundos { get; set; } = 300;

    /// <summary>Final 94: depois de confirmar, o banco devolve a transferência passado este atraso.</summary>
    public int AtrasoDevolucaoSegundos { get; set; } = 10;

    /// <summary>Probabilidade (0 a 1) de recusar uma liquidação comum.</summary>
    public double TaxaRecusa { get; set; }

    /// <summary>Probabilidade (0 a 1) de nunca responder uma liquidação comum.</summary>
    public double TaxaSilencio { get; set; }
}
