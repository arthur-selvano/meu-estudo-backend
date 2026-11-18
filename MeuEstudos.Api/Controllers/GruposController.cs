using System.Security.Claims;
using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuEstudos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/grupos")]
public sealed class GruposController : ControllerBase
{
    private readonly IGrupoEstudoService _grupoEstudoService;

    public GruposController(IGrupoEstudoService grupoEstudoService)
    {
        _grupoEstudoService = grupoEstudoService
            ?? throw new ArgumentNullException(nameof(grupoEstudoService));
    }

    /// <summary>Cria um grupo de estudo cujo dono é o usuário autenticado.</summary>
    [HttpPost]
    [ProducesResponseType<GrupoEstudoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GrupoEstudoResponse>> Criar(
        [FromBody] CriarGrupoRequest request,
        CancellationToken cancellationToken)
    {
        var grupo = await _grupoEstudoService.CriarAsync(ObterUsuarioId(), request, cancellationToken);

        return Ok(grupo);
    }

    /// <summary>Detalha um grupo, incluindo as colunas do Kanban com seus cards.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GrupoEstudoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GrupoEstudoResponse>> ObterDetalhes(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var grupo = await _grupoEstudoService.ObterDetalhesAsync(ObterUsuarioId(), id, cancellationToken);

        return Ok(grupo);
    }

    /// <summary>Lista os grupos em que o usuário é dono ou membro aceito.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GrupoEstudoResponse>>> ListarMeusGrupos(
        CancellationToken cancellationToken)
    {
        var grupos = await _grupoEstudoService.ListarDoUsuarioAsync(ObterUsuarioId(), cancellationToken);

        return Ok(grupos);
    }

    /// <summary>Lista os convites ainda pendentes para o usuário autenticado.</summary>
    [HttpGet("convites")]
    public async Task<ActionResult<IReadOnlyCollection<ConvitePendenteResponse>>> ListarConvitesPendentes(
        CancellationToken cancellationToken)
    {
        var convites = await _grupoEstudoService.ListarConvitesPendentesAsync(ObterUsuarioId(), cancellationToken);

        return Ok(convites);
    }

    /// <summary>Convida um usuário, pelo e-mail, para participar do grupo.</summary>
    [HttpPost("{id:guid}/convites")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConvidarMembro(
        [FromRoute] Guid id,
        [FromBody] ConvidarMembroRequest request,
        CancellationToken cancellationToken)
    {
        await _grupoEstudoService.ConvidarMembroAsync(ObterUsuarioId(), id, request, cancellationToken);

        return NoContent();
    }

    /// <summary>Aceita ou recusa o convite do usuário autenticado para o grupo.</summary>
    [HttpPatch("{id:guid}/convites/responder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResponderConvite(
        [FromRoute] Guid id,
        [FromBody] ResponderConviteRequest request,
        CancellationToken cancellationToken)
    {
        await _grupoEstudoService.ResponderConviteAsync(ObterUsuarioId(), id, request, cancellationToken);

        return NoContent();
    }

    /// <summary>Adiciona um card/tópico na coluna informada do grupo.</summary>
    [HttpPost("{id:guid}/topicos")]
    [ProducesResponseType<TopicoEstudoResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TopicoEstudoResponse>> AdicionarTopico(
        [FromRoute] Guid id,
        [FromBody] CriarTopicoRequest request,
        CancellationToken cancellationToken)
    {
        var topico = await _grupoEstudoService.AdicionarTopicoAsync(ObterUsuarioId(), id, request, cancellationToken);

        return Created($"/api/grupos/{id}/topicos/{topico.Id}", topico);
    }

    /// <summary>Alterna a conclusão de um tópico do grupo.</summary>
    [HttpPatch("{id:guid}/topicos/{topicoId:guid}/concluir")]
    public async Task<ActionResult<TopicoEstudoResponse>> AlternarConclusaoTopico(
        [FromRoute] Guid id,
        [FromRoute] Guid topicoId,
        CancellationToken cancellationToken)
    {
        var topico = await _grupoEstudoService.AlternarConclusaoTopicoAsync(
            ObterUsuarioId(),
            id,
            topicoId,
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
