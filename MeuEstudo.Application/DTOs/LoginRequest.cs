namespace MeuEstudo.Application.DTOs;

/// <summary>Credenciais para autenticação.</summary>
public sealed record LoginRequest(string Email, string Senha);
