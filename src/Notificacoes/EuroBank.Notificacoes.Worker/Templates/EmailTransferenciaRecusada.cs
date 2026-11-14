using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.DetalhesTransferencia;

namespace EuroBank.Notificacoes.Worker.Templates;

public static class EmailTransferenciaRecusada
{
    public static ConteudoEmail Montar(EventoTransferenciaRecusada evento)
    {
        var resumo = Exigir(evento.Resumo);
        return ModeloEmail.Montar(
            $"Transferência não realizada: {ValorPedido(resumo)} para {resumo.NomeBeneficiario}",
            resumo.NomeTitular,
            [
                $"Não foi possível fazer a sua transferência para {resumo.NomeBeneficiario}: {DescricoesMotivo.Descrever(evento.Motivo)}.",
                "Nada foi debitado da sua conta.",
            ],
            [
                .. Destino(resumo),
                ("Valor", ValorPedido(resumo)),
                .. Opcionais(resumo),
                .. Rodape(evento.TransferenciaId, evento.OcorridoEm),
            ]);
    }
}
