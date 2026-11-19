using MeuEstudo.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MeuEstudo.Api.Middlewares;

/// <summary>
/// Converte exceções não tratadas em respostas JSON padronizadas, sem expor stack trace ao cliente.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string ErroInterno = "Ocorreu um erro interno no servidor.";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, mensagem) = Mapear(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("Requisição rejeitada em {Method} {Path}: {Mensagem}", httpContext.Request.Method, httpContext.Request.Path, mensagem);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { erro = mensagem }, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Mensagem) Mapear(Exception exception) => exception switch
    {
        // Regra de negócio violada: mensagem segura e útil para o cliente.
        DomainException domainException
            => (StatusCodes.Status400BadRequest, domainException.Message),

        KeyNotFoundException
            => (StatusCodes.Status404NotFound, "Recurso não encontrado."),

        UnauthorizedAccessException
            => (StatusCodes.Status401Unauthorized, "Usuário não autenticado."),

        // Qualquer outra exceção: detalhes apenas no log, nunca na resposta.
        _ => (StatusCodes.Status500InternalServerError, ErroInterno)
    };
}
