using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;
using MeuEstudos.Domain.Repositories;

namespace MeuEstudos.Application.Services;

public sealed class PublicacaoService : IPublicacaoService
{
    private readonly IPublicacaoRepository _publicacaoRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public PublicacaoService(
        IPublicacaoRepository publicacaoRepository,
        IUsuarioRepository usuarioRepository)
    {
        _publicacaoRepository = publicacaoRepository
            ?? throw new ArgumentNullException(nameof(publicacaoRepository));
        _usuarioRepository = usuarioRepository
            ?? throw new ArgumentNullException(nameof(usuarioRepository));
    }

    public async Task<IReadOnlyCollection<PublicacaoResponse>> ListarAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var publicacoes = await _publicacaoRepository.ObterTodasAsync(cancellationToken);
        var usuarios = await ObterResumosAsync(
            publicacoes.SelectMany(UsuariosRelevantes).Distinct(),
            cancellationToken);

        return publicacoes
            .Select(publicacao => Mapear(publicacao, usuarios, usuarioId))
            .ToList();
    }

    public async Task<PublicacaoResponse> CriarAsync(
        Guid usuarioId,
        CriarPublicacaoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipo = ParseTipo(request.Tipo);
        var publicacao = new Publicacao(
            usuarioId,
            tipo,
            request.Conteudo ?? string.Empty,
            request.Titulo,
            request.LinkUrl,
            request.ArquivoUrl,
            request.ArquivoNome);

        await _publicacaoRepository.AdicionarAsync(publicacao, cancellationToken);

        var usuarios = await ObterResumosAsync(new[] { usuarioId }, cancellationToken);

        return Mapear(publicacao, usuarios, usuarioId);
    }

    public async Task<PublicacaoResponse> AlternarCurtidaAsync(
        Guid usuarioId,
        Guid publicacaoId,
        CancellationToken cancellationToken = default)
    {
        var publicacao = await ObterAsync(publicacaoId, cancellationToken);

        publicacao.AlternarCurtida(usuarioId);

        await _publicacaoRepository.AtualizarAsync(publicacao, cancellationToken);

        return await MapearComAutoresAsync(publicacao, usuarioId, cancellationToken);
    }

    public async Task<PublicacaoResponse> ComentarAsync(
        Guid usuarioId,
        Guid publicacaoId,
        ComentarPublicacaoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var publicacao = await ObterAsync(publicacaoId, cancellationToken);

        publicacao.Comentar(usuarioId, request.Conteudo ?? string.Empty);

        await _publicacaoRepository.AtualizarAsync(publicacao, cancellationToken);

        return await MapearComAutoresAsync(publicacao, usuarioId, cancellationToken);
    }

    public async Task<string?> ExcluirAsync(
        Guid usuarioId,
        Guid publicacaoId,
        CancellationToken cancellationToken = default)
    {
        var publicacao = await ObterAsync(publicacaoId, cancellationToken);

        publicacao.GarantirAutor(usuarioId);

        var arquivoUrl = publicacao.ArquivoUrl;

        await _publicacaoRepository.RemoverAsync(publicacao, cancellationToken);

        return arquivoUrl;
    }

    private async Task<Publicacao> ObterAsync(Guid publicacaoId, CancellationToken cancellationToken)
        => await _publicacaoRepository.ObterPorIdAsync(publicacaoId, cancellationToken)
           ?? throw new KeyNotFoundException("Publicação não encontrada.");

    private async Task<PublicacaoResponse> MapearComAutoresAsync(
        Publicacao publicacao,
        Guid solicitanteId,
        CancellationToken cancellationToken)
    {
        var usuarios = await ObterResumosAsync(
            UsuariosRelevantes(publicacao).Distinct(),
            cancellationToken);

        return Mapear(publicacao, usuarios, solicitanteId);
    }

    private Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
        => _usuarioRepository.ObterResumosPorIdsAsync(ids, cancellationToken);

    private static IEnumerable<Guid> UsuariosRelevantes(Publicacao publicacao)
        => publicacao.Comentarios
            .Select(comentario => comentario.AutorId)
            .Append(publicacao.AutorId);

    private static PublicacaoResponse Mapear(
        Publicacao publicacao,
        IReadOnlyDictionary<Guid, UsuarioResumo> usuarios,
        Guid solicitanteId)
    {
        var comentarios = publicacao.Comentarios
            .OrderBy(comentario => comentario.CriadoEm)
            .Select(comentario => new ComentarioPublicacaoResponse(
                comentario.Id,
                comentario.AutorId,
                ObterNome(usuarios, comentario.AutorId),
                ObterFoto(usuarios, comentario.AutorId),
                comentario.Conteudo,
                comentario.CriadoEm))
            .ToList();

        return new(
            publicacao.Id,
            publicacao.Tipo.ParaValorBanco(),
            publicacao.AutorId,
            ObterNome(usuarios, publicacao.AutorId),
            ObterFoto(usuarios, publicacao.AutorId),
            publicacao.Titulo,
            publicacao.Conteudo,
            publicacao.LinkUrl,
            publicacao.ArquivoUrl,
            publicacao.ArquivoNome,
            publicacao.CriadoEm,
            publicacao.Curtidas.Count,
            publicacao.Curtidas.Any(curtida => curtida.UsuarioId == solicitanteId),
            comentarios);
    }

    private static string ObterNome(
        IReadOnlyDictionary<Guid, UsuarioResumo> usuarios,
        Guid usuarioId)
        => usuarios.TryGetValue(usuarioId, out var usuario) ? usuario.Nome : string.Empty;

    private static string? ObterFoto(
        IReadOnlyDictionary<Guid, UsuarioResumo> usuarios,
        Guid usuarioId)
        => usuarios.TryGetValue(usuarioId, out var usuario) ? usuario.FotoUrl : null;

    private static TipoPublicacao ParseTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return TipoPublicacao.Estudo;

        return TipoPublicacaoExtensions.TentarConverter(tipo, out var valor)
            ? valor
            : throw new DomainException(
                "O tipo da publicação deve ser ESTUDO, AULA, LINK ou DOCUMENTO.");
    }
}
