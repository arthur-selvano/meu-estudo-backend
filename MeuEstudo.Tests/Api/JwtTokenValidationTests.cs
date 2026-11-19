using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuEstudo.Api.Services;
using MeuEstudo.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MeuEstudo.Tests.Api;

public sealed class JwtTokenValidationTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = new string('k', 64),
        Issuer = "MeuEstudo.Api",
        Audience = "MeuEstudo.Frontend",
        ExpiresMinutes = 120
    };

    private static string CriarToken(
        DateTime expiraEm,
        string? issuer = null,
        string? audience = null,
        string? chave = null,
        string algoritmo = SecurityAlgorithms.HmacSha256,
        bool assinar = true)
    {
        var credenciais = assinar
            ? new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave ?? Settings.Key)),
                algoritmo)
            : null;

        var token = new JwtSecurityToken(
            issuer: issuer ?? Settings.Issuer,
            audience: audience ?? Settings.Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "Fulano")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: expiraEm,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ClaimsPrincipal Validar(string token)
        => new JwtSecurityTokenHandler().ValidateToken(
            token,
            JwtTokenValidation.CriarParametros(Settings),
            out _);

    [Fact]
    public void TokenEmitidoPelaApi_EhAceitoEContemIdentificadorDoUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var token = new TokenService(Options.Create(Settings)).GerarToken(usuarioId, "Fulano");

        var principal = Validar(token);

        Assert.Equal("Fulano", principal.Identity?.Name);
        Assert.Equal(
            usuarioId.ToString(),
            principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.NotNull(principal.FindFirst(JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void TokenExpirado_EhRejeitado()
    {
        var token = CriarToken(DateTime.UtcNow.AddMinutes(-2));

        Assert.Throws<SecurityTokenExpiredException>(() => Validar(token));
    }

    [Fact]
    public void TokenComAssinaturaDeOutraChave_EhRejeitado()
    {
        var token = CriarToken(DateTime.UtcNow.AddMinutes(10), chave: new string('x', 64));

        // Dependendo do "kid", a validação da assinatura falha com
        // SecurityTokenSignatureKeyNotFoundException ou SecurityTokenInvalidSignatureException.
        Assert.ThrowsAny<SecurityTokenException>(() => Validar(token));
    }

    [Fact]
    public void TokenComEmissorErrado_EhRejeitado()
    {
        var token = CriarToken(DateTime.UtcNow.AddMinutes(10), issuer: "Invasor.Api");

        Assert.Throws<SecurityTokenInvalidIssuerException>(() => Validar(token));
    }

    [Fact]
    public void TokenComAudienciaErrada_EhRejeitado()
    {
        var token = CriarToken(DateTime.UtcNow.AddMinutes(10), audience: "Outro.Frontend");

        Assert.Throws<SecurityTokenInvalidAudienceException>(() => Validar(token));
    }

    [Fact]
    public void TokenComAlgoritmoDiferenteDeHS256_EhRejeitado()
    {
        // Mesma chave, algoritmo diferente: a lista ValidAlgorithms deve barrar.
        var token = CriarToken(
            DateTime.UtcNow.AddMinutes(10),
            algoritmo: SecurityAlgorithms.HmacSha512);

        Assert.ThrowsAny<SecurityTokenException>(() => Validar(token));
    }

    [Fact]
    public void TokenSemAssinatura_EhRejeitado()
    {
        var token = CriarToken(DateTime.UtcNow.AddMinutes(10), assinar: false);

        Assert.ThrowsAny<SecurityTokenException>(() => Validar(token));
    }

    [Fact]
    public void Configuracao_RejeitaChaveCurta()
    {
        var settings = new JwtSettings
        {
            Key = "curta",
            Issuer = Settings.Issuer,
            Audience = Settings.Audience
        };

        Assert.Throws<InvalidOperationException>(settings.Validar);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1441)]
    public void Configuracao_RejeitaValidadeForaDoIntervalo(int minutos)
    {
        var settings = new JwtSettings
        {
            Key = Settings.Key,
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            ExpiresMinutes = minutos
        };

        Assert.Throws<InvalidOperationException>(settings.Validar);
    }
}
