namespace MeuEstudos.Application.DTOs;

/// <summary>Representação de um tópico de estudo retornada pela aplicação.</summary>
public sealed record TopicoEstudoResponse(
    Guid Id,
    string Titulo,
    string? Descricao,
    DateOnly? DataPrevista,
    bool Concluido,
    Guid CriadoPorId,
    DateTime CriadoEm);
