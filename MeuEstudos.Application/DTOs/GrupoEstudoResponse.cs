namespace MeuEstudos.Application.DTOs;

/// <summary>Representação de um grupo de estudo retornada pela aplicação.</summary>
public sealed record GrupoEstudoResponse(
    Guid Id,
    string Titulo,
    string? Descricao,
    Guid DonoId,
    DateTime CriadoEm,
    IReadOnlyCollection<TopicoEstudoResponse> Topicos);
