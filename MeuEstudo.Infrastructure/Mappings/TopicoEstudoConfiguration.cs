using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeuEstudo.Infrastructure.Mappings;

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

        builder.Property(t => t.ColunaId)
            .HasColumnName("coluna_id")
            .IsRequired();

        builder.Property(t => t.CriadoPorId)
            .HasColumnName("criado_por_id")
            .IsRequired();

        builder.Property(t => t.ResponsavelId)
            .HasColumnName("responsavel_id");

        builder.Property(t => t.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("text");

        // O banco armazena valores em caixa alta ('BAIXA' | 'MEDIA' | 'ALTA').
        builder.Property(t => t.Prioridade)
            .HasColumnName("prioridade")
            .HasConversion(
                prioridade => prioridade.ParaValorBanco(),
                valor => PrioridadeTopicoExtensions.DeValorBanco(valor))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Ordem)
            .HasColumnName("ordem")
            .IsRequired();

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

        // Relação com colunas_kanban(id): coluna do card (ON DELETE CASCADE no init.sql).
        builder.HasOne<ColunaKanban>()
            .WithMany()
            .HasForeignKey(t => t.ColunaId)
            .HasConstraintName("fk_topico_coluna")
            .OnDelete(DeleteBehavior.Cascade);

        // Relação com usuarios(id): responsável pelo card (ON DELETE SET NULL no init.sql).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.ResponsavelId)
            .HasConstraintName("fk_topico_responsavel")
            .OnDelete(DeleteBehavior.SetNull);

        // Equivale ao idx_topicos_grupo.
        builder.HasIndex(t => t.GrupoId)
            .HasDatabaseName("idx_topicos_grupo");

        // Equivale ao idx_topicos_coluna_ordem.
        builder.HasIndex(t => new { t.ColunaId, t.Ordem })
            .HasDatabaseName("idx_topicos_coluna_ordem");

        // Equivale ao idx_topicos_responsavel.
        builder.HasIndex(t => t.ResponsavelId)
            .HasDatabaseName("idx_topicos_responsavel");

        // A relação com grupos_estudo(id) é configurada em GrupoEstudoConfiguration,
        // pois é necessária para a navegação Topicos com acesso por backing field.
    }
}
