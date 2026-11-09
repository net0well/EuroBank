namespace EuroBank.Contas.Application.Mappers;

/// <summary>Traduz o código de uma exceção de domínio no motivo de recusa que a saga entende.</summary>
public static class MotivoRecusaMapeador
{
    public const string ContaInexistente = "ContaInexistente";

    public static string DoCodigo(string codigo) => codigo switch
    {
        "saldo_insuficiente" => "SaldoInsuficiente",
        "conta_bloqueada" => "ContaBloqueada",
        "conta_encerrada" => "ContaEncerrada",
        "valor_invalido" => "ValorInvalido",
        "moeda_diferente" => "MoedaDiferente",
        _ => codigo,
    };
}
