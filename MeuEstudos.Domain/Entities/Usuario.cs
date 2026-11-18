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
    private const int FotoUrlMaxLength = 500;
    private const int BioMaxLength = 500;
    private const int CursoOuAreaMaxLength = 120;
    private const int InstituicaoMaxLength = 150;
    private const int TopicoMaxLength = 40;
    private const int MaxTopicos = 10;
    private const string FotoUrlPrefix = "/uploads/perfis/";

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string? FotoUrl { get; private set; }
    public string? Bio { get; private set; }
    public string? CursoOuArea { get; private set; }
    public string? Instituicao { get; private set; }
    public List<string> TopicosInteresse { get; private set; } = [];
    public List<string> TopicosAjuda { get; private set; } = [];
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

    /// <summary>
    /// Atualiza os dados públicos do perfil. Campos opcionais vazios são gravados como nulo.
    /// </summary>
    public void AtualizarPerfil(
        string nome,
        string? bio,
        string? cursoOuArea,
        string? instituicao,
        IEnumerable<string>? topicosInteresse,
        IEnumerable<string>? topicosAjuda)
    {
        DefinirNome(nome);

        Bio = NormalizarOpcional(bio, BioMaxLength, "A biografia");
        CursoOuArea = NormalizarOpcional(cursoOuArea, CursoOuAreaMaxLength, "O curso ou área");
        Instituicao = NormalizarOpcional(instituicao, InstituicaoMaxLength, "A instituição");
        TopicosInteresse = NormalizarTopicos(topicosInteresse);
        TopicosAjuda = NormalizarTopicos(topicosAjuda);
    }

    /// <summary>
    /// Define ou remove a foto de perfil. O caminho deve apontar para o diretório público de perfis.
    /// </summary>
    public void DefinirFotoUrl(string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
        {
            FotoUrl = null;
            return;
        }

        var fotoNormalizada = fotoUrl.Trim();

        if (fotoNormalizada.Length > FotoUrlMaxLength
            || !fotoNormalizada.StartsWith(FotoUrlPrefix, StringComparison.Ordinal))
            throw new DomainException("O caminho da foto de perfil é inválido.");

        FotoUrl = fotoNormalizada;
    }

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

    private static string? NormalizarOpcional(string? valor, int maxLength, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var normalizado = valor.Trim();

        if (normalizado.Length > maxLength)
            throw new DomainException($"{nomeCampo} deve ter no máximo {maxLength} caracteres.");

        return normalizado;
    }

    private static List<string> NormalizarTopicos(IEnumerable<string>? topicos)
    {
        if (topicos is null)
            return [];

        var normalizados = new List<string>();

        foreach (var topico in topicos)
        {
            if (string.IsNullOrWhiteSpace(topico))
                continue;

            var normalizado = topico.Trim();

            if (normalizado.Length > TopicoMaxLength)
                throw new DomainException($"Cada tópico deve ter no máximo {TopicoMaxLength} caracteres.");

            if (normalizados.Contains(normalizado, StringComparer.OrdinalIgnoreCase))
                continue;

            normalizados.Add(normalizado);
        }

        if (normalizados.Count > MaxTopicos)
            throw new DomainException($"Informe no máximo {MaxTopicos} tópicos por lista.");

        return normalizados;
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
