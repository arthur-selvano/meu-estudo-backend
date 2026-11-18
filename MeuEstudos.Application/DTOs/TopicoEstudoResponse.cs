namespace MeuEstudos.Application.DTOs;

/// <summary>Representação de um card/tópico de estudo retornada pela aplicação.</summary>
public sealed record TopicoEstudoResponse(
    Guid Id,
    string Titulo,
    string? Descricao,
    Guid ColunaId,
    Guid? ResponsavelId,
    string? ResponsavelNome,
    string? ResponsavelFotoUrl,
    string Prioridade,
    int Ordem,
    DateOnly? DataPrevista,
    bool Concluido,
    Guid CriadoPorId,
    DateTime CriadoEm);
