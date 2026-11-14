using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.DetalhesTransferencia;
using static EuroBank.Notificacoes.Worker.Templates.FormatacaoPtBr;

namespace EuroBank.Notificacoes.Worker.Templates;

public static class EmailTransferenciaDevolvida
{
    public static ConteudoEmail Montar(EventoTransferenciaDevolvida evento)
    {
        var resumo = Exigir(evento.Resumo);
        var devolvido = Dinheiro(evento.ValorDevolvido, evento.Moeda);
        var tarifa = Dinheiro(resumo.Tarifa, resumo.MoedaOrigem);
        return ModeloEmail.Montar(
            $"Transferência devolvida: {ValorEnviado(resumo)} para {resumo.NomeBeneficiario}",
            resumo.NomeTitular,
            [
                $"O banco do beneficiário devolveu a sua transferência para {resumo.NomeBeneficiario}.",
                $"O valor de {devolvido} voltou para a sua conta; a tarifa de {tarifa} não é devolvida.",
            ],
            [
                .. Destino(resumo),
                ("Valor enviado", ValorEnviado(resumo)),
                ("Valor devolvido", devolvido),
                ("Tarifa retida", tarifa),
                .. Opcionais(resumo),
                .. Rodape(evento.TransferenciaId, evento.OcorridoEm),
            ]);
    }
}
