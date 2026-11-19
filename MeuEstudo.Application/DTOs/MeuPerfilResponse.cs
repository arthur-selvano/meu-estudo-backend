namespace MeuEstudo.Application.DTOs;

/// <summary>Perfil completo do próprio usuário autenticado, incluindo o e-mail.</summary>
public sealed record MeuPerfilResponse(
    Guid Id,
    string Nome,
    string Email,
    string? FotoUrl,
    string? Bio,
    string? CursoOuArea,
    string? Instituicao,
    IReadOnlyCollection<string> TopicosInteresse,
    IReadOnlyCollection<string> TopicosAjuda,
    DateTime CriadoEm);
