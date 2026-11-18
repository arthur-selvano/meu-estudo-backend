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
    public DbSet<TopicoEstudo> TopicosEstudo => Set<TopicoEstudo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
