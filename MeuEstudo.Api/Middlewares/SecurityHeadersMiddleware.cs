namespace MeuEstudo.Api.Middlewares;

/// <summary>
/// Aplica cabeçalhos de segurança em todas as respostas da API. A API devolve
/// apenas JSON e arquivos estáticos; por isso a CSP pode ser bem restritiva
/// (a documentação Swagger, disponível só em Development, é excetuada).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        var documentacao = context.Request.Path.StartsWithSegments("/swagger")
            || context.Request.Path.StartsWithSegments("/openapi");

        if (!documentacao)
        {
            headers["Content-Security-Policy"] =
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        }

        // HSTS só faz sentido quando a requisição já chegou por HTTPS (o proxy
        // de produção termina o TLS e envia X-Forwarded-Proto).
        if (context.Request.IsHttps
            && (_environment.IsProduction() || _environment.IsStaging()))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Respostas de API podem conter dados privados (perfil, grupos, tokens).
        if (context.Request.Path.StartsWithSegments("/api"))
            headers["Cache-Control"] = "no-store";

        return _next(context);
    }
}
