using System.Security.Claims;
using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuEstudos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/topicos")]
public sealed class TopicosController : ControllerBase
{
    private readonly IGrupoEstudoService _grupoEstudoService;

    public TopicosController(IGrupoEstudoService grupoEstudoService)
    {
        _grupoEstudoService = grupoEstudoService
            ?? throw new ArgumentNullException(nameof(grupoEstudoService));
    }

    /// <summary>
    /// Move o card para a coluna e posição informadas. Se a coluna de destino for a última
    /// do quadro, o card é marcado automaticamente como concluído.
    /// </summary>
    [HttpPatch("{id:guid}/mover")]
    [ProducesResponseType<TopicoEstudoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TopicoEstudoResponse>> Mover(
        [FromRoute] Guid id,
        [FromBody] MoverTopicoRequest request,
        CancellationToken cancellationToken)
    {
        var topico = await _grupoEstudoService.MoverTopicoAsync(
            ObterUsuarioId(),
            id,
            request,
            cancellationToken);

        return Ok(topico);
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
