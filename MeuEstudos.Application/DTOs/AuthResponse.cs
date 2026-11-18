namespace MeuEstudos.Application.DTOs;

/// <summary>
/// Dados do usuário autenticado. A geração do token JWT é responsabilidade da camada de API.
/// </summary>
public sealed record AuthResponse(Guid Id, string Nome, string Email, string? FotoUrl);
