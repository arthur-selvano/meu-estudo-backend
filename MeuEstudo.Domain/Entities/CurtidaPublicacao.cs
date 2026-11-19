using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Curtida de um usuário em uma publicação. Entidade interna do agregado
/// <see cref="Publicacao"/>: a unicidade por usuário é garantida pelo agregado e pelo banco.
/// </summary>
public class CurtidaPublicacao
{
    public Guid Id { get; private set; }
    public Guid PublicacaoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected CurtidaPublicacao()
    {
    }

    internal CurtidaPublicacao(Guid publicacaoId, Guid usuarioId)
    {
        if (publicacaoId == Guid.Empty)
            throw new DomainException("A publicação da curtida é obrigatória.");

        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário da curtida é obrigatório.");

        Id = Guid.NewGuid();
        PublicacaoId = publicacaoId;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }
}
