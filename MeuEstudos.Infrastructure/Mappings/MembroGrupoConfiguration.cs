using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudos.Infrastructure.Mappings;

public sealed class MembroGrupoConfiguration : IEntityTypeConfiguration<MembroGrupo>
{
    public void Configure(EntityTypeBuilder<MembroGrupo> builder)
    {
        builder.ToTable("membros_grupo");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.GrupoId)
            .HasColumnName("grupo_id")
            .IsRequired();

        builder.Property(m => m.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        // O banco armazena o status como texto; o domínio trabalha com o enum StatusMembro.
        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.ConvidadoEm)
            .HasColumnName("convidado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.RespondidoEm)
            .HasColumnName("respondido_em")
            .HasColumnType("timestamp with time zone");

        // Relação com usuarios(id): FK do vínculo (ON DELETE CASCADE no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .HasConstraintName("fk_membro_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        // Equivale à CONSTRAINT uq_grupo_usuario UNIQUE (grupo_id, usuario_id).
        builder.HasIndex(m => new { m.GrupoId, m.UsuarioId })
            .IsUnique()
            .HasDatabaseName("uq_grupo_usuario");

        // Equivale ao idx_membros_usuario_status.
        builder.HasIndex(m => new { m.UsuarioId, m.Status })
            .HasDatabaseName("idx_membros_usuario_status");

        // A relação com grupos_estudo(id) é configurada em GrupoEstudoConfiguration,
        // pois é necessária para a navegação Membros com acesso por backing field.
    }
}
