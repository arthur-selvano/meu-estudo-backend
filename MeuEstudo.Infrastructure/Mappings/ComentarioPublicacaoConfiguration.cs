using MeuEstudo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudo.Infrastructure.Mappings;

public sealed class ComentarioPublicacaoConfiguration : IEntityTypeConfiguration<ComentarioPublicacao>
{
    public void Configure(EntityTypeBuilder<ComentarioPublicacao> builder)
    {
        builder.ToTable("comentarios_publicacao");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.PublicacaoId)
            .HasColumnName("publicacao_id")
            .IsRequired();

        builder.Property(c => c.AutorId)
            .HasColumnName("autor_id")
            .IsRequired();

        builder.Property(c => c.Conteudo)
            .HasColumnName("conteudo")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relação com usuarios(id): autor do comentário (ON DELETE RESTRICT no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.AutorId)
            .HasConstraintName("fk_comentario_autor")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.PublicacaoId, c.CriadoEm })
            .HasDatabaseName("idx_comentarios_publicacao");

        // A relação com publicacoes(id) é configurada em PublicacaoConfiguration,
        // pois é necessária para a navegação Comentarios com acesso por backing field.
    }
}
