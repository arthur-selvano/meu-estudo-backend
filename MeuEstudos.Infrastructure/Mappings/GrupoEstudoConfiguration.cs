using MeuEstudos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudos.Infrastructure.Mappings;

public sealed class GrupoEstudoConfiguration : IEntityTypeConfiguration<GrupoEstudo>
{
    public void Configure(EntityTypeBuilder<GrupoEstudo> builder)
    {
        builder.ToTable("grupos_estudo");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(g => g.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(g => g.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("text");

        builder.Property(g => g.DonoId)
            .HasColumnName("dono_id")
            .IsRequired();

        builder.Property(g => g.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relação com usuarios(id): dono do grupo (ON DELETE RESTRICT no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(g => g.DonoId)
            .HasConstraintName("fk_grupo_dono")
            .OnDelete(DeleteBehavior.Restrict);

        // Equivale ao idx_grupos_dono.
        builder.HasIndex(g => g.DonoId)
            .HasDatabaseName("idx_grupos_dono");

        // Relação 1:N com membros_grupo (ON DELETE CASCADE no init.sql).
        builder.HasMany(g => g.Membros)
            .WithOne()
            .HasForeignKey(m => m.GrupoId)
            .HasConstraintName("fk_membro_grupo")
            .OnDelete(DeleteBehavior.Cascade);

        // Relação 1:N com topicos_estudo (ON DELETE CASCADE no init.sql).
        builder.HasMany(g => g.Topicos)
            .WithOne()
            .HasForeignKey(t => t.GrupoId)
            .HasConstraintName("fk_topico_grupo")
            .OnDelete(DeleteBehavior.Cascade);

        // As propriedades Membros/Topicos são somente leitura e expõem AsReadOnly().
        // O EF Core deve popular/ler diretamente os backing fields privados.
        builder.Navigation(nameof(GrupoEstudo.Membros))
            .HasField("_membros")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(nameof(GrupoEstudo.Topicos))
            .HasField("_topicos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
