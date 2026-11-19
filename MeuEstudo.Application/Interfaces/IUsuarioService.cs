using MeuEstudo.Application.DTOs;

namespace MeuEstudo.Application.Interfaces;

/// <summary>
/// Casos de uso de visualização e edição do perfil do usuário.
/// </summary>
public interface IUsuarioService
{
    /// <summary>Retorna o perfil completo do próprio usuário autenticado.</summary>
    /// <exception cref="KeyNotFoundException">Usuário inexistente.</exception>
    Task<MeuPerfilResponse> ObterMeuPerfilAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o perfil público de qualquer usuário, sem dados privados.</summary>
    /// <exception cref="KeyNotFoundException">Usuário inexistente.</exception>
    Task<PerfilPublicoResponse> ObterPerfilPublicoAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>Lista os perfis públicos de todos os usuários para o diretório de membros.</summary>
    Task<IReadOnlyCollection<PerfilPublicoResponse>> ListarPerfisPublicosAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Atualiza o perfil do próprio usuário autenticado.</summary>
    /// <exception cref="MeuEstudo.Domain.Exceptions.DomainException">Dados inválidos.</exception>
    /// <exception cref="KeyNotFoundException">Usuário inexistente.</exception>
    Task<MeuPerfilResponse> AtualizarPerfilAsync(
        Guid usuarioId,
        AtualizarPerfilRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Registra o caminho da foto já armazenada e devolve a URL pública.</summary>
    /// <exception cref="MeuEstudo.Domain.Exceptions.DomainException">Caminho inválido.</exception>
    /// <exception cref="KeyNotFoundException">Usuário inexistente.</exception>
    Task<FotoPerfilResponse> DefinirFotoAsync(
        Guid usuarioId,
        string fotoUrl,
        CancellationToken cancellationToken = default);
}
