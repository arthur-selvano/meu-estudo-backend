using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Repositories;

namespace MeuEstudo.Tests.Fakes;

internal sealed class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly IReadOnlyDictionary<Guid, UsuarioResumo> _resumos;

    public FakeUsuarioRepository(IReadOnlyDictionary<Guid, UsuarioResumo>? resumos = null)
    {
        _resumos = resumos ?? new Dictionary<Guid, UsuarioResumo>();
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Usuario?>(null);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult<Usuario?>(null);

    public Task<IReadOnlyCollection<Usuario>> ObterTodosAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

    public Task<IReadOnlyDictionary<Guid, string>> ObterNomesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            _resumos
                .Where(par => ids.Contains(par.Key))
                .ToDictionary(par => par.Key, par => par.Value.Nome));

    public Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, UsuarioResumo>>(
            _resumos
                .Where(par => ids.Contains(par.Key))
                .ToDictionary(par => par.Key, par => par.Value));

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
