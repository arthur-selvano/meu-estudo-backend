using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Exceptions;
using MeuEstudos.Domain.Repositories;

namespace MeuEstudos.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int SenhaMinLength = 8;
    private const int SenhaMaxLength = 128;
    private const string CredenciaisInvalidas = "E-mail ou senha inválidos.";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<AuthResponse> RegistrarAsync(
        RegistrarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail do usuário é obrigatório.");

        ValidarSenha(request.Senha);

        if (await _usuarioRepository.ExisteEmailAsync(request.Email, cancellationToken))
            throw new DomainException("Já existe um usuário cadastrado com este e-mail.");

        var usuario = new Usuario(request.Nome, request.Email, _passwordHasher.Hash(request.Senha));

        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);

        return Mapear(usuario);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            throw new DomainException(CredenciaisInvalidas);

        var usuario = await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken);

        // A mesma mensagem para e-mail inexistente e senha incorreta evita revelar
        // quais e-mails estão cadastrados.
        if (usuario is null || !_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            throw new DomainException(CredenciaisInvalidas);

        return Mapear(usuario);
    }

    private static void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < SenhaMinLength)
            throw new DomainException($"A senha deve ter pelo menos {SenhaMinLength} caracteres.");

        if (senha.Length > SenhaMaxLength)
            throw new DomainException($"A senha deve ter no máximo {SenhaMaxLength} caracteres.");
    }

    private static AuthResponse Mapear(Usuario usuario)
        => new(usuario.Id, usuario.Nome, usuario.Email);
}
