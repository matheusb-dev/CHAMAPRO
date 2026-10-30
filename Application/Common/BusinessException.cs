namespace Chamapro.Api.Common;

/// <summary>
/// Exceção de negócio. Os Services lançam esta exceção e o ErrorHandlingMiddleware
/// a transforma na resposta de erro padrão (status, codigo, titulo, detalhe, erros).
/// </summary>
public class BusinessException : Exception
{
    public int Status { get; }
    public string Codigo { get; }
    public string Titulo { get; }
    public IReadOnlyList<ErroCampo>? Erros { get; }

    public BusinessException(int status, string codigo, string detalhe, string titulo = "Operação não permitida",
        IReadOnlyList<ErroCampo>? erros = null)
        : base(detalhe)
    {
        Status = status;
        Codigo = codigo;
        Titulo = titulo;
        Erros = erros;
    }

    /// <summary>400 - corpo ou campos inválidos.</summary>
    public static BusinessException Invalido(string codigo, string detalhe, IReadOnlyList<ErroCampo>? erros = null)
        => new(StatusCodes.Status400BadRequest, codigo, detalhe, "Requisição inválida", erros);

    /// <summary>403 - logado, mas sem permissão (perfil errado ou recurso de outro usuário).</summary>
    public static BusinessException Proibido(string codigo = "ACESSO_NEGADO", string detalhe = "Você não tem permissão para esta ação.")
        => new(StatusCodes.Status403Forbidden, codigo, detalhe, "Acesso negado");

    /// <summary>404 - recurso inexistente (ou não visível para o usuário).</summary>
    public static BusinessException NaoEncontrado(string codigo = "NAO_ENCONTRADO", string detalhe = "Recurso não encontrado.")
        => new(StatusCodes.Status404NotFound, codigo, detalhe, "Não encontrado");

    /// <summary>409 - conflito (e-mail já cadastrado, horário ocupado...).</summary>
    public static BusinessException Conflito(string codigo, string detalhe)
        => new(StatusCodes.Status409Conflict, codigo, detalhe, "Conflito");

    /// <summary>422 - regra de negócio violada (ex.: transição de status inválida).</summary>
    public static BusinessException Regra(string codigo, string detalhe)
        => new(StatusCodes.Status422UnprocessableEntity, codigo, detalhe, "Operação não permitida");
}

/// <summary>Item da lista "erros" da resposta de validação.</summary>
public record ErroCampo(string Campo, string Mensagem);
