using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Repositories;

namespace MeuEstudos.Application.Services;

public sealed class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository
            ?? throw new ArgumentNullException(nameof(usuarioRepository));
    }

    public async Task<MeuPerfilResponse> ObterMeuPerfilAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAsync(usuarioId, cancellationToken);

        return MapearMeuPerfil(usuario);
    }

    public async Task<PerfilPublicoResponse> ObterPerfilPublicoAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAsync(usuarioId, cancellationToken);

        return MapearPerfilPublico(usuario);
    }

    public async Task<IReadOnlyCollection<PerfilPublicoResponse>> ListarPerfisPublicosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync(cancellationToken);

        return usuarios.Select(MapearPerfilPublico).ToList();
    }

    public async Task<MeuPerfilResponse> AtualizarPerfilAsync(
        Guid usuarioId,
        AtualizarPerfilRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await ObterUsuarioAsync(usuarioId, cancellationToken);

        usuario.AtualizarPerfil(
            request.Nome,
            request.Bio,
            request.CursoOuArea,
            request.Instituicao,
            request.TopicosInteresse,
            request.TopicosAjuda);

        await _usuarioRepository.AtualizarAsync(usuario, cancellationToken);

        return MapearMeuPerfil(usuario);
    }

    public async Task<FotoPerfilResponse> DefinirFotoAsync(
        Guid usuarioId,
        string fotoUrl,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAsync(usuarioId, cancellationToken);

        usuario.DefinirFotoUrl(fotoUrl);

        await _usuarioRepository.AtualizarAsync(usuario, cancellationToken);

        return new FotoPerfilResponse(usuario.FotoUrl ?? string.Empty);
    }

    private async Task<Usuario> ObterUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken)
           ?? throw new KeyNotFoundException("Usuário não encontrado.");

    private static MeuPerfilResponse MapearMeuPerfil(Usuario usuario)
        => new(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.FotoUrl,
            usuario.Bio,
            usuario.CursoOuArea,
            usuario.Instituicao,
            usuario.TopicosInteresse.ToList(),
            usuario.TopicosAjuda.ToList(),
            usuario.CriadoEm);

    private static PerfilPublicoResponse MapearPerfilPublico(Usuario usuario)
        => new(
            usuario.Id,
            usuario.Nome,
            usuario.FotoUrl,
            usuario.Bio,
            usuario.CursoOuArea,
            usuario.Instituicao,
            usuario.TopicosInteresse.ToList(),
            usuario.TopicosAjuda.ToList(),
            usuario.CriadoEm);
}
