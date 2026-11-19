using System.Security.Claims;
using MeuEstudos.Api.Services;
using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuEstudos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/publicacoes")]
public sealed class PublicacoesController : ControllerBase
{
    private const long LimiteAnexoBytes = 11 * 1024 * 1024;

    private readonly IPublicacaoService _publicacaoService;
    private readonly IArquivoPublicacaoService _arquivoPublicacaoService;

    public PublicacoesController(
        IPublicacaoService publicacaoService,
        IArquivoPublicacaoService arquivoPublicacaoService)
    {
        _publicacaoService = publicacaoService
            ?? throw new ArgumentNullException(nameof(publicacaoService));
        _arquivoPublicacaoService = arquivoPublicacaoService
            ?? throw new ArgumentNullException(nameof(arquivoPublicacaoService));
    }

    /// <summary>Lista o feed da comunidade, do mais recente para o mais antigo.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PublicacaoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PublicacaoResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var publicacoes = await _publicacaoService.ListarAsync(ObterUsuarioId(), cancellationToken);

        return Ok(publicacoes);
    }

    /// <summary>
    /// Cria uma publicação. O conteúdo chega como formulário para permitir o envio opcional de anexo.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteAnexoBytes)]
    [ProducesResponseType<PublicacaoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicacaoResponse>> Criar(
        [FromForm] string? tipo,
        [FromForm] string? titulo,
        [FromForm] string? conteudo,
        [FromForm] string? linkUrl,
        [FromForm] IFormFile? arquivo,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        ArquivoPublicacaoSalvo? anexo = null;

        if (arquivo is not null)
            anexo = await _arquivoPublicacaoService.SalvarAsync(usuarioId, arquivo, cancellationToken);

        try
        {
            var publicacao = await _publicacaoService.CriarAsync(
                usuarioId,
                new CriarPublicacaoRequest(
                    tipo,
                    titulo,
                    conteudo,
                    linkUrl,
                    anexo?.Url,
                    anexo?.Nome),
                cancellationToken);

            return Created($"/api/publicacoes/{publicacao.Id}", publicacao);
        }
        catch
        {
            // Não deixa anexo órfão quando a validação da publicação falhar.
            if (anexo is not null)
                await _arquivoPublicacaoService.RemoverAsync(anexo.Url, cancellationToken);

            throw;
        }
    }

    /// <summary>Curte ou descurte uma publicação do usuário autenticado.</summary>
    [HttpPatch("{id:guid}/curtida")]
    [ProducesResponseType<PublicacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicacaoResponse>> AlternarCurtida(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var publicacao = await _publicacaoService.AlternarCurtidaAsync(
            ObterUsuarioId(),
            id,
            cancellationToken);

        return Ok(publicacao);
    }

    /// <summary>Adiciona um comentário à publicação e devolve o estado atualizado dela.</summary>
    [HttpPost("{id:guid}/comentarios")]
    [ProducesResponseType<PublicacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicacaoResponse>> Comentar(
        [FromRoute] Guid id,
        [FromBody] ComentarPublicacaoRequest request,
        CancellationToken cancellationToken)
    {
        var publicacao = await _publicacaoService.ComentarAsync(
            ObterUsuarioId(),
            id,
            request,
            cancellationToken);

        return Ok(publicacao);
    }

    /// <summary>Exclui uma publicação do próprio autor e remove o anexo, quando existir.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var arquivoUrl = await _publicacaoService.ExcluirAsync(
            ObterUsuarioId(),
            id,
            cancellationToken);

        if (arquivoUrl is not null)
        {
            try
            {
                await _arquivoPublicacaoService.RemoverAsync(arquivoUrl, cancellationToken);
            }
            catch
            {
                // Um arquivo órfão não invalida a exclusão já persistida.
            }
        }

        return NoContent();
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
