using MeuEstudos.Domain.Entities;

namespace MeuEstudos.Domain.Repositories;

/// <summary>
/// Contrato de persistência de <see cref="Usuario"/>.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
