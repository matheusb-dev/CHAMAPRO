using Chamapro.Api.Common;
using Microsoft.AspNetCore.Http;

namespace Chamapro.Api.Services;

/// <summary>
/// Fábrica dos erros do módulo. Se o construtor da BusinessException da P2 for diferente,
/// o ajuste é só neste arquivo. Assumido: BusinessException(int status, string codigo, string titulo, string detalhe).
/// </summary>
internal static class PedidoErros
{
    public static BusinessException PedidoNaoEncontrado()
        => new(StatusCodes.Status404NotFound, "PEDIDO_NAO_ENCONTRADO", "Recurso não encontrado", "Pedido não encontrado.");

    public static BusinessException OrcamentoNaoEncontrado()
        => new(StatusCodes.Status404NotFound, "ORCAMENTO_NAO_ENCONTRADO", "Recurso não encontrado", "Orçamento não encontrado.");

    public static BusinessException SemPermissao(string detalhe)
        => new(StatusCodes.Status403Forbidden, "ACESSO_NEGADO", "Acesso negado", detalhe);

    public static BusinessException Regra(string codigo, string detalhe)
        => new(StatusCodes.Status422UnprocessableEntity, codigo, "Operação não permitida", detalhe);

    public static BusinessException StatusInvalido(string detalhe)
        => Regra("PEDIDO_STATUS_INVALIDO", detalhe);
}
