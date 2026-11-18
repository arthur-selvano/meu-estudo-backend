namespace MeuEstudos.Application.DTOs;

/// <summary>Dados para criação de um grupo de estudo.</summary>
public sealed record CriarGrupoRequest(string Titulo, string? Descricao = null);
