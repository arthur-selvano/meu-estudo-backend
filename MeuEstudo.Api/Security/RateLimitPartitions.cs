using System.Security.Claims;

namespace MeuEstudo.Api.Security;

/// <summary>
/// Chaves de particionamento do rate limiter. Endpoints autenticados usam o
/// identificador do usuário; os demais usam o IP do cliente (já resolvido via
/// ForwardedHeaders quando atrás de um proxy confiável).
/// </summary>
public static class RateLimitPartitions
{
    public static string PorIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();

        return string.IsNullOrWhiteSpace(ip) ? "ip:desconhecido" : $"ip:{ip}";
    }

    public static string PorUsuarioOuIp(HttpContext context)
    {
        var usuarioId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(usuarioId, out var id) ? $"usuario:{id}" : PorIp(context);
    }
}
