using EuroBank.Shared.Exceptions;

namespace EuroBank.Transferencias.Application.Exceptions;

public sealed class ExcecaoFiltroInvalido(string filtro, string valor)
    : ExcecaoValidacao("filtro_invalido", $"Valor '{valor}' inválido para o filtro {filtro}.");
