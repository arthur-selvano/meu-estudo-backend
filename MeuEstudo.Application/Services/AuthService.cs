using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;
using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Exceptions;
using MeuEstudo.Domain.Repositories;

namespace MeuEstudo.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int SenhaMinLength = 8;
    private const int SenhaMaxLength = 128;
    private const int EmailMaxLength = 150;
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

        if (request.Email.Trim().Length > EmailMaxLength)
            throw new DomainException("O e-mail informado é inválido.");

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

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Senha)
            || request.Email.Trim().Length > EmailMaxLength
            || request.Senha.Length > SenhaMaxLength)
        {
            throw new DomainException(CredenciaisInvalidas);
        }

        var usuario = await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken);

        // A mesma mensagem para e-mail inexistente e senha incorreta evita revelar
        // quais e-mails estão cadastrados. Quando o usuário não existe, executamos
        // um hash descartável para que o tempo de resposta não denuncie a ausência.
        if (usuario is null)
        {
            _passwordHasher.Hash(request.Senha);
            throw new DomainException(CredenciaisInvalidas);
        }

        if (!_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            throw new DomainException(CredenciaisInvalidas);

        // Atualiza silenciosamente hashes emitidos com parâmetros antigos.
        if (_passwordHasher.PrecisaRehash(usuario.SenhaHash))
        {
            usuario.AlterarSenhaHash(_passwordHasher.Hash(request.Senha));
            await _usuarioRepository.AtualizarAsync(usuario, cancellationToken);
        }

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
        => new(usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl);
}
