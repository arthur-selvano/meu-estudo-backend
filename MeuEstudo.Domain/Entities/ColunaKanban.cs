using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Coluna do quadro Kanban de um grupo. Entidade interna do agregado <see cref="GrupoEstudo"/>:
/// é criada automaticamente junto com o grupo e só deve ser alterada por meio dele.
/// </summary>
public class ColunaKanban
{
    private const int TituloMaxLength = 80;

    public Guid Id { get; private set; }
    public Guid GrupoId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public int Ordem { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected ColunaKanban()
    {
    }

    internal ColunaKanban(Guid grupoId, string titulo, int ordem)
    {
        if (grupoId == Guid.Empty)
            throw new DomainException("O grupo da coluna é obrigatório.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título da coluna é obrigatório.");

        var tituloNormalizado = titulo.Trim();

        if (tituloNormalizado.Length > TituloMaxLength)
            throw new DomainException($"O título da coluna deve ter no máximo {TituloMaxLength} caracteres.");

        if (ordem < 0)
            throw new DomainException("A ordem da coluna não pode ser negativa.");

        Id = Guid.NewGuid();
        GrupoId = grupoId;
        Titulo = tituloNormalizado;
        Ordem = ordem;
        CriadoEm = DateTime.UtcNow;
    }
}
