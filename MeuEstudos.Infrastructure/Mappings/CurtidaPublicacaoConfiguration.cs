using MeuEstudos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudos.Infrastructure.Mappings;

public sealed class CurtidaPublicacaoConfiguration : IEntityTypeConfiguration<CurtidaPublicacao>
{
    public void Configure(EntityTypeBuilder<CurtidaPublicacao> builder)
    {
        builder.ToTable("curtidas_publicacao");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.PublicacaoId)
            .HasColumnName("publicacao_id")
            .IsRequired();

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relação com usuarios(id): usuário que curtiu (ON DELETE CASCADE no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .HasConstraintName("fk_curtida_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        // Equivale à CONSTRAINT uq_publicacao_usuario UNIQUE (publicacao_id, usuario_id).
        builder.HasIndex(c => new { c.PublicacaoId, c.UsuarioId })
            .IsUnique()
            .HasDatabaseName("uq_publicacao_usuario");

        builder.HasIndex(c => c.UsuarioId)
            .HasDatabaseName("idx_curtidas_usuario");

        // A relação com publicacoes(id) é configurada em PublicacaoConfiguration,
        // pois é necessária para a navegação Curtidas com acesso por backing field.
    }
}
