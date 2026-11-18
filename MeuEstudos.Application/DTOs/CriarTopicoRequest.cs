namespace MeuEstudos.Application.DTOs;

/// <summary>Dados para criação de um tópico de estudo.</summary>
public sealed record CriarTopicoRequest(
    string Titulo,
    string? Descricao = null,
    DateOnly? DataPrevista = null);
