using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

public sealed class ExcecaoModeloNaoEncontrado(int versao)
    : ExcecaoNaoEncontrado("modelo_nao_encontrado", $"A versão {versao} do modelo não existe.");
