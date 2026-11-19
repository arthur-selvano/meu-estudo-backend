using MeuEstudo.Application.DTOs;

namespace MeuEstudo.Application.Interfaces;

/// <summary>
/// Casos de uso de cadastro e autenticação de usuários.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Cadastra um usuário, garantindo e-mail único e armazenando apenas o hash da senha.
    /// </summary>
    /// <exception cref="MeuEstudo.Domain.Exceptions.DomainException">E-mail já cadastrado ou dados inválidos.</exception>
    Task<AuthResponse> RegistrarAsync(
        RegistrarUsuarioRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida as credenciais informadas e retorna os dados do usuário autenticado.
    /// </summary>
    /// <exception cref="MeuEstudo.Domain.Exceptions.DomainException">Credenciais inválidas.</exception>
    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
