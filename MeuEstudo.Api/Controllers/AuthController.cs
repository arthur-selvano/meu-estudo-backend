using MeuEstudo.Api.Responses;
using MeuEstudo.Api.Services;
using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeuEstudo.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <summary>Cadastra um usuário e devolve o token JWT com os dados do usuário.</summary>
    [HttpPost("registrar")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokenResponse>> Registrar(
        [FromBody] RegistrarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await _authService.RegistrarAsync(request, cancellationToken);

        return Ok(MontarResposta(usuario));
    }

    /// <summary>Autentica o usuário e devolve o token JWT com os dados do usuário.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await _authService.LoginAsync(request, cancellationToken);

        return Ok(MontarResposta(usuario));
    }

    private AuthTokenResponse MontarResposta(AuthResponse usuario)
        => new(_tokenService.GerarToken(usuario.Id, usuario.Nome), usuario);
}
