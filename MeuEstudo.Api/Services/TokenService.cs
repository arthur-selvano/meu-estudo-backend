using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuEstudo.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MeuEstudo.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings?.Value
            ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public string GerarToken(Guid usuarioId, string nome)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("O identificador do usuário é obrigatório.", nameof(usuarioId));

        var agora = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, nome),
            // jti permite identificar a emissão do token em logs e futuras listas de revogação.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: agora,
            expires: agora.AddMinutes(_jwtSettings.ExpiresMinutes),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
