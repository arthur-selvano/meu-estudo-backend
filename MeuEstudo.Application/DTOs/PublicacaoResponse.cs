namespace MeuEstudo.Application.DTOs;

/// <summary>Comentário de uma publicação, com os dados públicos do autor.</summary>
public sealed record ComentarioPublicacaoResponse(
    Guid Id,
    Guid AutorId,
    string AutorNome,
    string? AutorFotoUrl,
    string Conteudo,
    DateTime CriadoEm);

/// <summary>Publicação do feed da comunidade, incluindo interações.</summary>
public sealed record PublicacaoResponse(
    Guid Id,
    string Tipo,
    Guid AutorId,
    string AutorNome,
    string? AutorFotoUrl,
    string? Titulo,
    string Conteudo,
    string? LinkUrl,
    string? ArquivoUrl,
    string? ArquivoNome,
    DateTime CriadoEm,
    int Curtidas,
    bool CurtidoPorMim,
    IReadOnlyCollection<ComentarioPublicacaoResponse> Comentarios);
