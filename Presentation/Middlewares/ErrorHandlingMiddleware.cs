using System.Text.Json;
using Chamapro.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chamapro.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            await Escrever(context, ex.Status, ex.Codigo, ex.Titulo, ex.Message, ex.Erros);
        }
        catch (BadHttpRequestException ex)
        {
            var grande = ex.StatusCode == StatusCodes.Status413PayloadTooLarge;
            await Escrever(context, ex.StatusCode,
                grande ? "ARQUIVO_MUITO_GRANDE" : "REQUISICAO_INVALIDA",
                grande ? "Arquivo muito grande" : "Requisição inválida",
                grande ? "O arquivo enviado é maior que o permitido." : "A requisição está malformada.",
                null);
        }

        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
            await Escrever(context, StatusCodes.Status500InternalServerError, "ERRO_INTERNO",
                "Erro inesperado", "Ocorreu um erro inesperado. Tente novamente mais tarde.", null);
        }
    }

    private static async Task Escrever(HttpContext context, int status, string codigo, string titulo,
        string detalhe, IReadOnlyList<ErroCampo>? erros)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var corpo = new Dictionary<string, object?>
        {
            ["status"] = status,
            ["codigo"] = codigo,
            ["titulo"] = titulo,
            ["detalhe"] = detalhe
        };
        if (erros is { Count: > 0 })
            corpo["erros"] = erros.Select(e => new { campo = e.Campo, mensagem = e.Mensagem });

        await context.Response.WriteAsync(JsonSerializer.Serialize(corpo, JsonOpcoes));
    }

    private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

    public static IActionResult RespostaValidacao(ActionContext contexto)
    {
        var erros = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(er => new
            {
                campo = string.IsNullOrWhiteSpace(e.Key) ? "corpo" : JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                mensagem = string.IsNullOrWhiteSpace(er.ErrorMessage) ? "Valor inválido." : er.ErrorMessage
            }))
            .ToList();

        return new ObjectResult(new
        {
            status = StatusCodes.Status400BadRequest,
            codigo = "VALIDACAO",
            titulo = "Requisição inválida",
            detalhe = "Um ou mais campos são inválidos.",
            erros
        })
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }
}
