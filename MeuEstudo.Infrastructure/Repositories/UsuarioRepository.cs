using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Repositories;
using MeuEstudo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MeuEstudo.Infrastructure.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = Usuario.NormalizarEmail(email);

        return _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Usuario>> ObterTodosAsync(
        CancellationToken cancellationToken = default)
        => await _context.Usuarios
            .AsNoTracking()
            .OrderBy(usuario => usuario.Nome)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> ObterNomesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idsDistintos = ids.Distinct().ToArray();

        if (idsDistintos.Length == 0)
            return new Dictionary<Guid, string>();

        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => idsDistintos.Contains(usuario.Id))
            .ToDictionaryAsync(usuario => usuario.Id, usuario => usuario.Nome, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idsDistintos = ids.Distinct().ToArray();

        if (idsDistintos.Length == 0)
            return new Dictionary<Guid, UsuarioResumo>();

        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => idsDistintos.Contains(usuario.Id))
            .Select(usuario => new UsuarioResumo(usuario.Id, usuario.Nome, usuario.FotoUrl))
            .ToDictionaryAsync(usuario => usuario.Id, cancellationToken);
    }

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = Usuario.NormalizarEmail(email);

        return _context.Usuarios.AnyAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(usuario).State == EntityState.Detached)
            _context.Usuarios.Update(usuario);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
