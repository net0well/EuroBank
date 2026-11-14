using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Application.Exceptions;

public sealed class ExcecaoFiltroInvalido(string filtro, string valor)
    : ExcecaoValidacao("filtro_invalido", $"Valor '{valor}' inválido para o filtro {filtro}.");
