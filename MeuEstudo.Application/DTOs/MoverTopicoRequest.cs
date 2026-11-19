namespace MeuEstudo.Application.DTOs;

/// <summary>Coluna e posição de destino de um card no Kanban.</summary>
public sealed record MoverTopicoRequest(Guid ColunaId, int Ordem);
