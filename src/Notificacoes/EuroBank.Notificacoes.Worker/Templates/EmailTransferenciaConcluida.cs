using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.DetalhesTransferencia;

namespace EuroBank.Notificacoes.Worker.Templates;

public static class EmailTransferenciaConcluida
{
    public static ConteudoEmail Montar(EventoTransferenciaConcluida evento)
    {
        var resumo = Exigir(evento.Resumo);
        var reconciliada = evento.Motivo == "Reconciliada";
        var paragrafos = new List<string> { $"Sua transferência para {resumo.NomeBeneficiario} foi concluída." };
        if (reconciliada)
        {
            paragrafos.Add(
                "Ela tinha ficado em análise porque o banco do beneficiário confirmou o pagamento depois de o valor " +
                "ter voltado para a sua conta. Na reconciliação, o valor e a tarifa foram debitados novamente.");
        }
        else if (resumo.Tipo == "CambioInterno")
        {
            paragrafos.Add("O valor convertido já está na conta de destino.");
        }

        var titulo = reconciliada ? "Transferência concluída após reconciliação" : "Transferência concluída";
        return ModeloEmail.Montar(
            $"{titulo}: {ValorEnviado(resumo)} para {resumo.NomeBeneficiario}",
            resumo.NomeTitular,
            paragrafos,
            [
                .. Destino(resumo),
                ("Valor enviado", ValorEnviado(resumo)),
                .. Debito(resumo),
                .. Opcionais(resumo),
                .. Rodape(evento.TransferenciaId, evento.OcorridoEm),
            ]);
    }
}
