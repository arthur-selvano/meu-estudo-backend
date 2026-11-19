namespace MeuEstudo.Application.DTOs;

/// <summary>Dados editáveis do perfil do usuário autenticado.</summary>
public sealed record AtualizarPerfilRequest(
    string Nome,
    string? Bio,
    string? CursoOuArea,
    string? Instituicao,
    IReadOnlyCollection<string>? TopicosInteresse,
    IReadOnlyCollection<string>? TopicosAjuda);
