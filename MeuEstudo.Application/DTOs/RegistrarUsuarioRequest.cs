namespace MeuEstudo.Application.DTOs;

/// <summary>Dados necessários para o cadastro de um novo usuário.</summary>
public sealed record RegistrarUsuarioRequest(string Nome, string Email, string Senha);
