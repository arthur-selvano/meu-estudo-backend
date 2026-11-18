using MeuEstudos.Application.DTOs;

namespace MeuEstudos.Application.Interfaces;

/// <summary>
/// Casos de uso de grupos de estudo, convites e tópicos/cards do Kanban.
/// </summary>
public interface IGrupoEstudoService
{
    Task<GrupoEstudoResponse> CriarAsync(
        Guid usuarioId,
        CriarGrupoRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GrupoEstudoResponse>> ListarDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<GrupoEstudoResponse> ObterDetalhesAsync(
        Guid usuarioId,
        Guid grupoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConvitePendenteResponse>> ListarConvitesPendentesAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task ConvidarMembroAsync(
        Guid usuarioId,
        Guid grupoId,
        ConvidarMembroRequest request,
        CancellationToken cancellationToken = default);

    Task ResponderConviteAsync(
        Guid usuarioId,
        Guid grupoId,
        ResponderConviteRequest request,
        CancellationToken cancellationToken = default);

    Task<TopicoEstudoResponse> AdicionarTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        CriarTopicoRequest request,
        CancellationToken cancellationToken = default);

    Task<TopicoEstudoResponse> AlternarConclusaoTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        Guid topicoId,
        CancellationToken cancellationToken = default);

    Task<TopicoEstudoResponse> MoverTopicoAsync(
        Guid usuarioId,
        Guid topicoId,
        MoverTopicoRequest request,
        CancellationToken cancellationToken = default);

    Task<TopicoEstudoResponse> AlterarResponsavelTopicoAsync(
        Guid usuarioId,
        Guid topicoId,
        AlterarResponsavelTopicoRequest request,
        CancellationToken cancellationToken = default);
}
