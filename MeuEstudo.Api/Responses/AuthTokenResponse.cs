using MeuEstudo.Application.DTOs;

namespace MeuEstudo.Api.Responses;

/// <summary>Token JWT emitido e dados públicos do usuário autenticado.</summary>
public sealed record AuthTokenResponse(string Token, AuthResponse Usuario);
