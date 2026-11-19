using System.Security.Claims;
using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;
using MeuEstudo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeuEstudo.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class PerfisController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IFotoPerfilService _fotoPerfilService;

    public PerfisController(
        IUsuarioService usuarioService,
        IFotoPerfilService fotoPerfilService)
    {
        _usuarioService = usuarioService
            ?? throw new ArgumentNullException(nameof(usuarioService));
        _fotoPerfilService = fotoPerfilService
            ?? throw new ArgumentNullException(nameof(fotoPerfilService));
    }

    /// <summary>Retorna o perfil completo do usuário autenticado.</summary>
    [HttpGet("me/profile")]
    [ProducesResponseType<MeuPerfilResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeuPerfilResponse>> ObterMeuPerfil(
        CancellationToken cancellationToken)
    {
        var perfil = await _usuarioService.ObterMeuPerfilAsync(ObterUsuarioId(), cancellationToken);

        return Ok(perfil);
    }

    /// <summary>Lista os perfis públicos de todos os usuários para o diretório de membros.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PerfilPublicoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PerfilPublicoResponse>>> ListarPerfisPublicos(
        CancellationToken cancellationToken)
    {
        var perfis = await _usuarioService.ListarPerfisPublicosAsync(cancellationToken);

        return Ok(perfis);
    }

    /// <summary>Retorna o perfil público de qualquer usuário, sem dados privados.</summary>
    [HttpGet("{id:guid}/profile")]
    [ProducesResponseType<PerfilPublicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilPublicoResponse>> ObterPerfilPublico(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var perfil = await _usuarioService.ObterPerfilPublicoAsync(id, cancellationToken);

        return Ok(perfil);
    }

    /// <summary>Atualiza foto, bio, curso/instituição e tópicos do próprio usuário.</summary>
    [HttpPut("me/profile")]
    [ProducesResponseType<MeuPerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeuPerfilResponse>> AtualizarPerfil(
        [FromBody] AtualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        var perfil = await _usuarioService.AtualizarPerfilAsync(
            ObterUsuarioId(),
            request,
            cancellationToken);

        return Ok(perfil);
    }

    /// <summary>Envia e grava uma nova foto de perfil, substituindo a anterior.</summary>
    [HttpPost("me/profile/foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [EnableRateLimiting("upload")]
    [ProducesResponseType<FotoPerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<FotoPerfilResponse>> EnviarFoto(
        [FromForm] IFormFile foto,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        var perfilAtual = await _usuarioService.ObterMeuPerfilAsync(usuarioId, cancellationToken);
        var novaUrl = await _fotoPerfilService.SalvarAsync(usuarioId, foto, cancellationToken);

        FotoPerfilResponse resposta;

        try
        {
            resposta = await _usuarioService.DefinirFotoAsync(usuarioId, novaUrl, cancellationToken);
        }
        catch
        {
            // Não deixa arquivo órfão se a persistência falhar.
            await _fotoPerfilService.RemoverAsync(novaUrl, cancellationToken);
            throw;
        }

        // A remoção da antiga é best-effort: falhar aqui não pode invalidar a nova foto.
        await _fotoPerfilService.RemoverAsync(perfilAtual.FotoUrl, cancellationToken);

        return Ok(resposta);
    }

    /// <summary>
    /// Identidade do usuário vem exclusivamente do token JWT, nunca do corpo da requisição.
    /// </summary>
    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(valor, out var usuarioId)
            ? usuarioId
            : throw new UnauthorizedAccessException("O token não contém um identificador de usuário válido.");
    }
}
