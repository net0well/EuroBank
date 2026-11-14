using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.FormatacaoPtBr;

namespace EuroBank.Notificacoes.Worker.Templates;

/// <summary>Linhas e valores da tabela de detalhes que os e-mails compartilham.</summary>
public static class DetalhesTransferencia
{
    /// <summary>O que o cliente pediu, na moeda em que pediu.</summary>
    public static string ValorPedido(ResumoTransferencia resumo) => Dinheiro(resumo.Valor, resumo.MoedaDoValor);

    /// <summary>O que o destino recebeu; antes da cotação, o valor pedido.</summary>
    public static string ValorEnviado(ResumoTransferencia resumo) =>
        resumo.ValorDestino is { } destino ? Dinheiro(destino, resumo.MoedaDestino) : ValorPedido(resumo);

    public static IEnumerable<(string Rotulo, string Valor)> Destino(ResumoTransferencia resumo)
    {
        yield return ("Beneficiário", resumo.NomeBeneficiario);
        yield return ("IBAN de destino", resumo.IbanDestino);
    }

    /// <summary>"1.237,00 EUR (1.234,50 EUR + tarifa de 2,50 EUR)"; nada antes da cotação.</summary>
    public static IEnumerable<(string Rotulo, string Valor)> Debito(ResumoTransferencia resumo)
    {
        if (resumo.ValorDebitado is { } debitado)
        {
            yield return (
                "Debitado da conta",
                $"{Dinheiro(debitado + resumo.Tarifa, resumo.MoedaOrigem)} " +
                $"({Dinheiro(debitado, resumo.MoedaOrigem)} + tarifa de {Dinheiro(resumo.Tarifa, resumo.MoedaOrigem)})");
        }
    }

    /// <summary>Cotação (quando houve câmbio) e descrição (quando o cliente informou).</summary>
    public static IEnumerable<(string Rotulo, string Valor)> Opcionais(ResumoTransferencia resumo)
    {
        if (resumo.Taxa is { } taxa)
        {
            yield return ("Cotação", $"1 {resumo.MoedaOrigem} = {Taxa(taxa)} {resumo.MoedaDestino}");
        }

        if (!string.IsNullOrWhiteSpace(resumo.Descricao))
        {
            yield return ("Descrição", resumo.Descricao);
        }
    }

    public static IEnumerable<(string Rotulo, string Valor)> Rodape(Guid transferenciaId, DateTimeOffset ocorridoEm)
    {
        yield return ("Data", DataHora(ocorridoEm));
        yield return ("Transferência", transferenciaId.ToString());
    }

    /// <summary>Os eventos publicados antes das notificações não têm resumo; o manipulador nem chega a montar o e-mail.</summary>
    public static ResumoTransferencia Exigir(ResumoTransferencia? resumo) =>
        resumo ?? throw new InvalidOperationException("O evento não tem o resumo da transferência.");
}
