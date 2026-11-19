using MeuEstudo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudo.Infrastructure.Mappings;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.Nome)
            .HasColumnName("nome")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.SenhaHash)
            .HasColumnName("senha_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.FotoUrl)
            .HasColumnName("foto_url")
            .HasMaxLength(500);

        builder.Property(u => u.Bio)
            .HasColumnName("bio")
            .HasMaxLength(500);

        builder.Property(u => u.CursoOuArea)
            .HasColumnName("curso_ou_area")
            .HasMaxLength(120);

        builder.Property(u => u.Instituicao)
            .HasColumnName("instituicao")
            .HasMaxLength(150);

        builder.Property(u => u.TopicosInteresse)
            .HasColumnName("topicos_interesse")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(u => u.TopicosAjuda)
            .HasColumnName("topicos_ajuda")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(u => u.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Equivale ao UNIQUE (email) declarado em init.sql.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("usuarios_email_key");
    }
}
