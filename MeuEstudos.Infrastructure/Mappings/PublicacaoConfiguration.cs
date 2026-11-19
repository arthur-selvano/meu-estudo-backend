using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudos.Infrastructure.Mappings;

public sealed class PublicacaoConfiguration : IEntityTypeConfiguration<Publicacao>
{
    public void Configure(EntityTypeBuilder<Publicacao> builder)
    {
        builder.ToTable("publicacoes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.AutorId)
            .HasColumnName("autor_id")
            .IsRequired();

        // O banco armazena valores em caixa alta ('ESTUDO' | 'AULA' | 'LINK' | 'DOCUMENTO').
        builder.Property(p => p.Tipo)
            .HasColumnName("tipo")
            .HasConversion(
                tipo => tipo.ParaValorBanco(),
                valor => TipoPublicacaoExtensions.DeValorBanco(valor))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(150);

        builder.Property(p => p.Conteudo)
            .HasColumnName("conteudo")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(p => p.LinkUrl)
            .HasColumnName("link_url")
            .HasMaxLength(500);

        builder.Property(p => p.ArquivoUrl)
            .HasColumnName("arquivo_url")
            .HasMaxLength(500);

        builder.Property(p => p.ArquivoNome)
            .HasColumnName("arquivo_nome")
            .HasMaxLength(255);

        builder.Property(p => p.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relação com usuarios(id): autor da publicação (ON DELETE RESTRICT no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.AutorId)
            .HasConstraintName("fk_publicacao_autor")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Comentarios)
            .WithOne()
            .HasForeignKey(c => c.PublicacaoId)
            .HasConstraintName("fk_comentario_publicacao")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Curtidas)
            .WithOne()
            .HasForeignKey(c => c.PublicacaoId)
            .HasConstraintName("fk_curtida_publicacao")
            .OnDelete(DeleteBehavior.Cascade);

        // As propriedades são somente leitura e expõem AsReadOnly().
        builder.Navigation(p => p.Comentarios)
            .HasField("_comentarios")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(p => p.Curtidas)
            .HasField("_curtidas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.AutorId)
            .HasDatabaseName("idx_publicacoes_autor");

        builder.HasIndex(p => p.CriadoEm)
            .HasDatabaseName("idx_publicacoes_criado_em");
    }
}
