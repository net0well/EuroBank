using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Templates;

/// <summary>
/// Moldura comum dos e-mails: saudação, parágrafos e uma tabela de detalhes, em HTML simples (estilos inline, que é o
/// que os clientes de e-mail respeitam) e em texto. Todo texto vindo do cliente (nomes, descrição) passa pelo encoder
/// no HTML; no assunto, quebras de linha e caracteres de controle viram espaço.
/// </summary>
public static partial class ModeloEmail
{
    private const string Assinatura = "EuroBank (laboratório de estudos). Mensagem automática, não responda.";

    // Mantém acentos legíveis no HTML; ainda codifica <, >, &, aspas e o que for perigoso.
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static ConteudoEmail Montar(
        string assunto,
        string nomeTitular,
        IReadOnlyList<string> paragrafos,
        IEnumerable<(string Rotulo, string Valor)> detalhes)
    {
        var linhas = detalhes.ToList();
        var assuntoLimpo = EspacosEControles().Replace(assunto, " ").Trim();
        return new ConteudoEmail(assuntoLimpo, Html(assuntoLimpo, nomeTitular, paragrafos, linhas), Texto(nomeTitular, paragrafos, linhas));
    }

    private static string Texto(string nomeTitular, IReadOnlyList<string> paragrafos, List<(string Rotulo, string Valor)> detalhes)
    {
        var texto = new StringBuilder();
        texto.Append("Olá, ").Append(nomeTitular).Append(".\n\n");
        foreach (var paragrafo in paragrafos)
        {
            texto.Append(paragrafo).Append("\n\n");
        }

        foreach (var (rotulo, valor) in detalhes)
        {
            texto.Append(rotulo).Append(": ").Append(valor).Append('\n');
        }

        texto.Append('\n').Append(Assinatura).Append('\n');
        return texto.ToString();
    }

    private static string Html(
        string assunto,
        string nomeTitular,
        IReadOnlyList<string> paragrafos,
        List<(string Rotulo, string Valor)> detalhes)
    {
        var corpo = new StringBuilder();
        corpo.Append("<p style=\"margin:0 0 16px\">Olá, ").Append(Encoder.Encode(nomeTitular)).Append(".</p>\n");
        foreach (var paragrafo in paragrafos)
        {
            corpo.Append("<p style=\"margin:0 0 16px\">").Append(Encoder.Encode(paragrafo)).Append("</p>\n");
        }

        corpo.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"6\" cellspacing=\"0\" ")
            .Append("style=\"border-collapse:collapse;font-size:14px\">\n");
        foreach (var (rotulo, valor) in detalhes)
        {
            corpo.Append("<tr><td style=\"color:#52606d;border-bottom:1px solid #e4e7eb;white-space:nowrap\">")
                .Append(Encoder.Encode(rotulo))
                .Append("</td><td style=\"border-bottom:1px solid #e4e7eb\">")
                .Append(Encoder.Encode(valor))
                .Append("</td></tr>\n");
        }

        corpo.Append("</table>");

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><title>{Encoder.Encode(assunto)}</title></head>
            <body style="margin:0;padding:24px;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#1f2933">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff">
            <tr><td style="padding:20px 24px;background:#0b3d91;color:#ffffff;font-size:18px;font-weight:bold">EuroBank</td></tr>
            <tr><td style="padding:24px;font-size:15px;line-height:1.5">
            {corpo}
            </td></tr>
            <tr><td style="padding:16px 24px;font-size:12px;color:#7b8794">{Encoder.Encode(Assinatura)}</td></tr>
            </table>
            </body>
            </html>
            """;
    }

    [GeneratedRegex(@"[\s\p{Cc}]+")]
    private static partial Regex EspacosEControles();
}
