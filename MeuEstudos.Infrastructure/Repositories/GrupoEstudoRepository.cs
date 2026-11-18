using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Repositories;
using MeuEstudos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MeuEstudos.Infrastructure.Repositories;

public sealed class GrupoEstudoRepository : IGrupoEstudoRepository
{
    private readonly AppDbContext _context;

    public GrupoEstudoRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Carrega o agregado completo (membros e tópicos), pois as regras do domínio dependem
    /// dessas coleções para validar convite duplicado e permissões.
    /// </summary>
    public Task<GrupoEstudo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ComMembrosETopicos()
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GrupoEstudo?> ObterPorTopicoIdAsync(Guid topicoId, CancellationToken cancellationToken = default)
        => ComMembrosETopicos()
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Topicos.Any(t => t.Id == topicoId), cancellationToken);

    public async Task<IReadOnlyCollection<GrupoEstudo>> ObterPorDonoAsync(
        Guid donoId,
        CancellationToken cancellationToken = default)
        => await ComMembrosETopicos()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(g => g.DonoId == donoId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<GrupoEstudo>> ObterDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
        => await ComMembrosETopicos()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(g => g.DonoId == usuarioId
                        || g.Membros.Any(m => m.UsuarioId == usuarioId && m.Status == StatusMembro.Aceito))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<GrupoEstudo>> ObterComConvitePendenteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
        => await _context.GruposEstudo
            .AsNoTracking()
            .Include(g => g.Membros)
            .Where(g => g.Membros.Any(m => m.UsuarioId == usuarioId && m.Status == StatusMembro.Pendente))
            .ToListAsync(cancellationToken);

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.GruposEstudo.AnyAsync(g => g.Id == id, cancellationToken);

    public async Task AdicionarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default)
    {
        await _context.GruposEstudo.AddAsync(grupo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(grupo).State == EntityState.Detached)
            _context.GruposEstudo.Update(grupo);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<GrupoEstudo> ComMembrosETopicos()
        => _context.GruposEstudo
            .Include(g => g.Membros)
            .Include(g => g.Colunas)
            .Include(g => g.Topicos);
}
