namespace MeuEstudos.Application.DTOs;

/// <summary>Perfil visível para os demais usuários. Nunca inclui e-mail ou dados privados.</summary>
public sealed record PerfilPublicoResponse(
    Guid Id,
    string Nome,
    string? FotoUrl,
    string? Bio,
    string? CursoOuArea,
    string? Instituicao,
    IReadOnlyCollection<string> TopicosInteresse,
    IReadOnlyCollection<string> TopicosAjuda,
    DateTime CriadoEm);
