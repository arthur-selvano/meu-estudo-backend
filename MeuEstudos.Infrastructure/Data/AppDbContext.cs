using MeuEstudos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeuEstudos.Infrastructure.Data;

/// <summary>
/// Contexto do EF Core responsável pelo mapeamento do domínio para o PostgreSQL.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<GrupoEstudo> GruposEstudo => Set<GrupoEstudo>();
    public DbSet<MembroGrupo> MembrosGrupo => Set<MembroGrupo>();
    public DbSet<ColunaKanban> ColunasKanban => Set<ColunaKanban>();
    public DbSet<TopicoEstudo> TopicosEstudo => Set<TopicoEstudo>();
    public DbSet<Publicacao> Publicacoes => Set<Publicacao>();
    public DbSet<ComentarioPublicacao> ComentariosPublicacao => Set<ComentarioPublicacao>();
    public DbSet<CurtidaPublicacao> CurtidasPublicacao => Set<CurtidaPublicacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
