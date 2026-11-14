namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>E-mail pronto para o SMTP. Id é o id do evento: vira o Message-ID, então um reenvio tem o mesmo Message-ID.</summary>
public sealed record EmailParaEnviar(Guid Id, string Destinatario, ConteudoEmail Conteudo);
