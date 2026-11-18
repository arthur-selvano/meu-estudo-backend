using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Domain.Entities;

/// <summary>
/// Usuário da aplicação. Entidade de referência para donos de grupos, membros e autores de tópicos.
/// </summary>
public class Usuario
{
    private const int NomeMaxLength = 120;
    private const int EmailMaxLength = 150;
    private const int SenhaHashMaxLength = 255;

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected Usuario()
    {
    }

    public Usuario(string nome, string email, string senhaHash)
    {
        Id = Guid.NewGuid();
        DefinirNome(nome);
        DefinirEmail(email);
        DefinirSenhaHash(senhaHash);
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Altera o nome do usuário.
    /// </summary>
    public void AlterarNome(string nome) => DefinirNome(nome);

    /// <summary>
    /// Substitui o hash da senha. O hash já deve vir calculado pela camada de aplicação/infraestrutura.
    /// </summary>
    public void AlterarSenhaHash(string senhaHash) => DefinirSenhaHash(senhaHash);

    private void DefinirNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do usuário é obrigatório.");

        var nomeNormalizado = nome.Trim();

        if (nomeNormalizado.Length > NomeMaxLength)
            throw new DomainException($"O nome do usuário deve ter no máximo {NomeMaxLength} caracteres.");

        Nome = nomeNormalizado;
    }

    /// <summary>Normaliza o e-mail (trim + minúsculas) para armazenamento e busca.</summary>
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    private void DefinirEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do usuário é obrigatório.");

        var emailNormalizado = NormalizarEmail(email);

        if (emailNormalizado.Length > EmailMaxLength)
            throw new DomainException($"O e-mail do usuário deve ter no máximo {EmailMaxLength} caracteres.");

        if (!EmailValido(emailNormalizado))
            throw new DomainException("O e-mail informado é inválido.");

        Email = emailNormalizado;
    }

    private void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("O hash da senha é obrigatório.");

        if (senhaHash.Length > SenhaHashMaxLength)
            throw new DomainException($"O hash da senha deve ter no máximo {SenhaHashMaxLength} caracteres.");

        SenhaHash = senhaHash;
    }

    private static bool EmailValido(string email)
    {
        var indiceArroba = email.IndexOf('@');

        return indiceArroba > 0
               && indiceArroba == email.LastIndexOf('@')
               && indiceArroba < email.Length - 1
               && email.IndexOf(' ') < 0;
    }
}
