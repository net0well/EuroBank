using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>A versão ativa não vai para a sombra: para trocar, ative outra versão.</summary>
public sealed class ExcecaoTransicaoModeloInvalida(int versao, StatusModelo atual, StatusModelo pedido)
    : ExcecaoConflito(
        "transicao_modelo_invalida",
        $"A versão {versao} do modelo está {atual} e não pode passar a {pedido}.");
