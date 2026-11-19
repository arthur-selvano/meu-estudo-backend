using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Repositories;
using MeuEstudos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MeuEstudos.Infrastructure.Repositories;

public sealed class PublicacaoRepository : IPublicacaoRepository
{
    private readonly AppDbContext _context;

    public PublicacaoRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyCollection<Publicacao>> ObterTodasAsync(
        CancellationToken cancellationToken = default)
        => await ComComentariosECurtidas()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(publicacao => publicacao.CriadoEm)
            .ToListAsync(cancellationToken);

    public Task<Publicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ComComentariosECurtidas()
            .AsSplitQuery()
            .FirstOrDefaultAsync(publicacao => publicacao.Id == id, cancellationToken);

    public async Task AdicionarAsync(
        Publicacao publicacao,
        CancellationToken cancellationToken = default)
    {
        await _context.Publicacoes.AddAsync(publicacao, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(
        Publicacao publicacao,
        CancellationToken cancellationToken = default)
    {
        if (_context.Entry(publicacao).State == EntityState.Detached)
            _context.Publicacoes.Update(publicacao);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(
        Publicacao publicacao,
        CancellationToken cancellationToken = default)
    {
        _context.Publicacoes.Remove(publicacao);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Publicacao> ComComentariosECurtidas()
        => _context.Publicacoes
            .Include(publicacao => publicacao.Comentarios)
            .Include(publicacao => publicacao.Curtidas);
}
