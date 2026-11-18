using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Domain.Entities;

/// <summary>
/// Tópico de estudo pertencente a um grupo. Entidade interna do agregado <see cref="GrupoEstudo"/>:
/// só deve ser criada ou alterada por meio do próprio grupo.
/// </summary>
public class TopicoEstudo
{
    private const int TituloMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid GrupoId { get; private set; }
    public Guid CriadoPorId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public DateOnly? DataPrevista { get; private set; }
    public bool Concluido { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected TopicoEstudo()
    {
    }

    internal TopicoEstudo(Guid grupoId, Guid criadoPorId, string titulo, string? descricao, DateOnly? dataPrevista)
    {
        if (grupoId == Guid.Empty)
            throw new DomainException("O grupo do tópico é obrigatório.");

        if (criadoPorId == Guid.Empty)
            throw new DomainException("O autor do tópico é obrigatório.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título do tópico é obrigatório.");

        var tituloNormalizado = titulo.Trim();

        if (tituloNormalizado.Length > TituloMaxLength)
            throw new DomainException($"O título do tópico deve ter no máximo {TituloMaxLength} caracteres.");

        Id = Guid.NewGuid();
        GrupoId = grupoId;
        CriadoPorId = criadoPorId;
        Titulo = tituloNormalizado;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        DataPrevista = dataPrevista;
        Concluido = false;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Inverte a situação de conclusão do tópico.
    /// </summary>
    internal void AlternarConclusao()
    {
        Concluido = !Concluido;
    }
}
