using MeuEstudo.Application.DTOs;

namespace MeuEstudo.Application.Interfaces;

/// <summary>
/// Casos de uso do feed da comunidade: publicações, curtidas e comentários.
/// </summary>
public interface IPublicacaoService
{
    /// <summary>Lista o feed do mais recente para o mais antigo.</summary>
    Task<IReadOnlyCollection<PublicacaoResponse>> ListarAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>Cria uma publicação em nome do usuário autenticado.</summary>
    Task<PublicacaoResponse> CriarAsync(
        Guid usuarioId,
        CriarPublicacaoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Alterna a curtida do usuário e devolve a publicação atualizada.</summary>
    Task<PublicacaoResponse> AlternarCurtidaAsync(
        Guid usuarioId,
        Guid publicacaoId,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona um comentário e devolve a publicação atualizada.</summary>
    Task<PublicacaoResponse> ComentarAsync(
        Guid usuarioId,
        Guid publicacaoId,
        ComentarPublicacaoRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exclui uma publicação do próprio autor e devolve o caminho do anexo, quando existir,
    /// para que a API remova o arquivo armazenado.
    /// </summary>
    Task<string?> ExcluirAsync(
        Guid usuarioId,
        Guid publicacaoId,
        CancellationToken cancellationToken = default);
}
