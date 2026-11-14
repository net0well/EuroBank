using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.DetalhesTransferencia;

namespace EuroBank.Notificacoes.Worker.Templates;

public static class EmailTransferenciaCancelada
{
    public static ConteudoEmail Montar(EventoTransferenciaCancelada evento)
    {
        var resumo = Exigir(evento.Resumo);
        return ModeloEmail.Montar(
            $"Transferência cancelada: {ValorPedido(resumo)} para {resumo.NomeBeneficiario}",
            resumo.NomeTitular,
            [
                $"Sua transferência para {resumo.NomeBeneficiario} foi cancelada: {DescricoesMotivo.Descrever(evento.Motivo)}.",
                "Se algum valor chegou a ser debitado, ele voltou para a sua conta junto com a tarifa.",
            ],
            [
                .. Destino(resumo),
                ("Valor", ValorPedido(resumo)),
                .. Opcionais(resumo),
                .. Rodape(evento.TransferenciaId, evento.OcorridoEm),
            ]);
    }
}
