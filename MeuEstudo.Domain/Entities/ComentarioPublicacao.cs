using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Comentário de uma publicação da comunidade. Entidade interna do agregado
/// <see cref="Publicacao"/>: só deve ser criada por meio do próprio agregado.
/// </summary>
public class ComentarioPublicacao
{
    private const int ConteudoMaxLength = 500;

    public Guid Id { get; private set; }
    public Guid PublicacaoId { get; private set; }
    public Guid AutorId { get; private set; }
    public string Conteudo { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected ComentarioPublicacao()
    {
    }

    internal ComentarioPublicacao(Guid publicacaoId, Guid autorId, string conteudo)
    {
        if (publicacaoId == Guid.Empty)
            throw new DomainException("A publicação do comentário é obrigatória.");

        if (autorId == Guid.Empty)
            throw new DomainException("O autor do comentário é obrigatório.");

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException("O comentário não pode estar vazio.");

        var textoNormalizado = conteudo.Trim();

        if (textoNormalizado.Length > ConteudoMaxLength)
            throw new DomainException($"O comentário deve ter no máximo {ConteudoMaxLength} caracteres.");

        Id = Guid.NewGuid();
        PublicacaoId = publicacaoId;
        AutorId = autorId;
        Conteudo = textoNormalizado;
        CriadoEm = DateTime.UtcNow;
    }
}
