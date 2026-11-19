namespace MeuEstudo.Application.DTOs;

/// <summary>Coluna do Kanban, com seus cards já ordenados pela posição.</summary>
public sealed record ColunaKanbanResponse(
    Guid Id,
    string Titulo,
    int Ordem,
    IReadOnlyCollection<TopicoEstudoResponse> Topicos);
