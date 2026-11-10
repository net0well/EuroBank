namespace EuroBank.Transferencias.Domain.Enums;

public enum TipoTransferencia
{
    /// <summary>Para um IBAN de outro banco, liquidada pelo banco externo.</summary>
    Externa = 1,

    /// <summary>Entre contas EuroBank de moedas diferentes: débito na origem, crédito convertido no destino.</summary>
    CambioInterno = 2,
}
