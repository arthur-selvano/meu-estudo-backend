using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MeuEstudo.Api.Settings;

/// <summary>
/// Centraliza os parâmetros de validação do JWT para que a API e os testes
/// usem exatamente as mesmas regras (assinatura, algoritmo, emissor, público e expiração).
/// </summary>
public static class JwtTokenValidation
{
    public static TokenValidationParameters CriarParametros(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validar();

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            // Restringe a aceitação ao HS256: impede confusão de algoritmo
            // (por exemplo, tokens "none" ou assinados com outro algoritmo).
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }
}
