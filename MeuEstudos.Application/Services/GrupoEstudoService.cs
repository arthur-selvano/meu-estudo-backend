using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;
using MeuEstudos.Domain.Repositories;

namespace MeuEstudos.Application.Services;

public sealed class GrupoEstudoService : IGrupoEstudoService
{
    private readonly IGrupoEstudoRepository _grupoEstudoRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public GrupoEstudoService(
        IGrupoEstudoRepository grupoEstudoRepository,
        IUsuarioRepository usuarioRepository)
    {
        _grupoEstudoRepository = grupoEstudoRepository
            ?? throw new ArgumentNullException(nameof(grupoEstudoRepository));
        _usuarioRepository = usuarioRepository
            ?? throw new ArgumentNullException(nameof(usuarioRepository));
    }

    public async Task<GrupoEstudoResponse> CriarAsync(
        Guid usuarioId,
        CriarGrupoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = new GrupoEstudo(request.Titulo, request.Descricao, usuarioId);

        await _grupoEstudoRepository.AdicionarAsync(grupo, cancellationToken);

        return MapearGrupo(grupo);
    }

    public async Task<IReadOnlyCollection<GrupoEstudoResponse>> ListarDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var grupos = await _grupoEstudoRepository.ObterDoUsuarioAsync(usuarioId, cancellationToken);

        return grupos.Select(MapearGrupo).ToList();
    }

    public async Task<IReadOnlyCollection<ConvitePendenteResponse>> ListarConvitesPendentesAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var grupos = await _grupoEstudoRepository.ObterComConvitePendenteAsync(usuarioId, cancellationToken);

        return grupos
            .Select(grupo =>
            {
                var convite = grupo.Membros.Single(
                    m => m.UsuarioId == usuarioId && m.Status == StatusMembro.Pendente);

                return new ConvitePendenteResponse(
                    grupo.Id,
                    grupo.Titulo,
                    grupo.DonoId,
                    convite.ConvidadoEm);
            })
            .ToList();
    }

    public async Task ConvidarMembroAsync(
        Guid usuarioId,
        Guid grupoId,
        ConvidarMembroRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail do usuário a ser convidado é obrigatório.");

        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        var convidado = await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken)
            ?? throw new DomainException("Usuário não encontrado para o e-mail informado.");

        // O overload com solicitante mantém a verificação de "somente o dono convida" no domínio.
        grupo.ConvidarMembro(usuarioId, convidado.Id);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);
    }

    public async Task ResponderConviteAsync(
        Guid usuarioId,
        Guid grupoId,
        ResponderConviteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.ResponderConvite(usuarioId, request.Aceitar);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);
    }

    public async Task<TopicoEstudoResponse> AdicionarTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        CriarTopicoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        var topico = grupo.AdicionarTopico(
            usuarioId,
            request.Titulo,
            request.Descricao,
            request.DataPrevista);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        return MapearTopico(topico);
    }

    public async Task<TopicoEstudoResponse> AlternarConclusaoTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        Guid topicoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.AlternarConclusaoTopico(topicoId, usuarioId);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        var topico = grupo.Topicos.Single(t => t.Id == topicoId);

        return MapearTopico(topico);
    }

    private async Task<GrupoEstudo> ObterGrupoAsync(Guid grupoId, CancellationToken cancellationToken)
        => await _grupoEstudoRepository.ObterPorIdAsync(grupoId, cancellationToken)
           ?? throw new KeyNotFoundException("Grupo de estudo não encontrado.");

    private static GrupoEstudoResponse MapearGrupo(GrupoEstudo grupo)
        => new(
            grupo.Id,
            grupo.Titulo,
            grupo.Descricao,
            grupo.DonoId,
            grupo.CriadoEm,
            grupo.Topicos.Select(MapearTopico).ToList());

    private static TopicoEstudoResponse MapearTopico(TopicoEstudo topico)
        => new(
            topico.Id,
            topico.Titulo,
            topico.Descricao,
            topico.DataPrevista,
            topico.Concluido,
            topico.CriadoPorId,
            topico.CriadoEm);
}
