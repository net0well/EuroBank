namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Mensagem com um tipo que o Extrato não conhece (ex.: evento novo em Contas). É ignorada com alerta.</summary>
public sealed record EventoDesconhecido(string Tipo);
