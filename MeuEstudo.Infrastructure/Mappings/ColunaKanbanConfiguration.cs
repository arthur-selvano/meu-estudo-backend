using MeuEstudo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudo.Infrastructure.Mappings;

public sealed class ColunaKanbanConfiguration : IEntityTypeConfiguration<ColunaKanban>
{
    public void Configure(EntityTypeBuilder<ColunaKanban> builder)
    {
        builder.ToTable("colunas_kanban");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.GrupoId)
            .HasColumnName("grupo_id")
            .IsRequired();

        builder.Property(c => c.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(c => c.Ordem)
            .HasColumnName("ordem")
            .IsRequired();

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Equivale ao idx_colunas_grupo_ordem.
        builder.HasIndex(c => new { c.GrupoId, c.Ordem })
            .HasDatabaseName("idx_colunas_grupo_ordem");

        // A relação com grupos_estudo(id) é configurada em GrupoEstudoConfiguration,
        // pois é necessária para a navegação Colunas com acesso por backing field.
    }
}
