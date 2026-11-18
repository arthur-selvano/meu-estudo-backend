using MeuEstudos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudos.Infrastructure.Mappings;

public sealed class TopicoEstudoConfiguration : IEntityTypeConfiguration<TopicoEstudo>
{
    public void Configure(EntityTypeBuilder<TopicoEstudo> builder)
    {
        builder.ToTable("topicos_estudo");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.GrupoId)
            .HasColumnName("grupo_id")
            .IsRequired();

        builder.Property(t => t.CriadoPorId)
            .HasColumnName("criado_por_id")
            .IsRequired();

        builder.Property(t => t.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("text");

        builder.Property(t => t.DataPrevista)
            .HasColumnName("data_prevista")
            .HasColumnType("date");

        builder.Property(t => t.Concluido)
            .HasColumnName("concluido")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relação com usuarios(id): autor do tópico (ON DELETE RESTRICT no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.CriadoPorId)
            .HasConstraintName("fk_topico_criador")
            .OnDelete(DeleteBehavior.Restrict);

        // Equivale ao idx_topicos_grupo.
        builder.HasIndex(t => t.GrupoId)
            .HasDatabaseName("idx_topicos_grupo");

        // A relação com grupos_estudo(id) é configurada em GrupoEstudoConfiguration,
        // pois é necessária para a navegação Topicos com acesso por backing field.
    }
}
