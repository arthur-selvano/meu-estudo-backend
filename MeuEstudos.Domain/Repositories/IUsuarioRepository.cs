using MeuEstudos.Domain.Entities;

namespace MeuEstudos.Domain.Repositories;

/// <summary>
/// Contrato de persistência de <see cref="Usuario"/>.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca os nomes dos usuários correspondentes aos ids informados. Ids desconhecidos são omitidos.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ObterNomesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca os dados públicos (nome e foto) dos usuários correspondentes aos ids informados.
    /// Ids desconhecidos são omitidos.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
