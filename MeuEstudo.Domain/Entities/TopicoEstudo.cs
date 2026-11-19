using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Card/tópico de estudo pertencente a um grupo. Entidade interna do agregado <see cref="GrupoEstudo"/>:
/// só deve ser criada ou alterada por meio do próprio grupo.
/// </summary>
public class TopicoEstudo
{
    private const int TituloMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid GrupoId { get; private set; }
    public Guid ColunaId { get; private set; }
    public Guid CriadoPorId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public PrioridadeTopico Prioridade { get; private set; }
    public int Ordem { get; private set; }
    public DateOnly? DataPrevista { get; private set; }
    public bool Concluido { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected TopicoEstudo()
    {
    }

    internal TopicoEstudo(
        Guid grupoId,
        Guid colunaId,
        Guid criadoPorId,
        string titulo,
        string? descricao,
        Guid? responsavelId,
        PrioridadeTopico prioridade,
        int ordem,
        DateOnly? dataPrevista,
        bool concluido = false)
    {
        if (grupoId == Guid.Empty)
            throw new DomainException("O grupo do tópico é obrigatório.");

        if (colunaId == Guid.Empty)
            throw new DomainException("A coluna do tópico é obrigatória.");

        if (criadoPorId == Guid.Empty)
            throw new DomainException("O autor do tópico é obrigatório.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título do tópico é obrigatório.");

        var tituloNormalizado = titulo.Trim();

        if (tituloNormalizado.Length > TituloMaxLength)
            throw new DomainException($"O título do tópico deve ter no máximo {TituloMaxLength} caracteres.");

        if (ordem < 0)
            throw new DomainException("A ordem do tópico não pode ser negativa.");

        if (!Enum.IsDefined(prioridade))
            throw new DomainException("A prioridade do tópico é inválida.");

        Id = Guid.NewGuid();
        GrupoId = grupoId;
        ColunaId = colunaId;
        CriadoPorId = criadoPorId;
        ResponsavelId = responsavelId;
        Titulo = tituloNormalizado;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        Prioridade = prioridade;
        Ordem = ordem;
        DataPrevista = dataPrevista;
        Concluido = concluido;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Inverte a situação de conclusão do tópico.
    /// </summary>
    internal void AlternarConclusao()
    {
        Concluido = !Concluido;
    }

    /// <summary>
    /// Define ou remove o responsável pelo card. A validação de quem pode ser
    /// responsável é responsabilidade do agregado <see cref="GrupoEstudo"/>.
    /// </summary>
    internal void DefinirResponsavel(Guid? responsavelId)
    {
        ResponsavelId = responsavelId;
    }

    /// <summary>
    /// Move o card para outra coluna/posição. O tópico é considerado concluído
    /// quando a coluna de destino é a última do quadro.
    /// </summary>
    internal void MoverPara(Guid colunaId, int ordem, bool concluido)
    {
        if (colunaId == Guid.Empty)
            throw new DomainException("A coluna de destino é obrigatória.");

        if (ordem < 0)
            throw new DomainException("A ordem do tópico não pode ser negativa.");

        ColunaId = colunaId;
        Ordem = ordem;
        Concluido = concluido;
    }
}
