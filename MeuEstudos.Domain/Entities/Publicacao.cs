using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Domain.Entities;

/// <summary>
/// Publicação da comunidade de estudos. Pode ser um pedido de estudo, uma aula,
/// um link ou um documento, e concentra curtidas e comentários.
/// </summary>
public class Publicacao
{
    private const int ConteudoMaxLength = 1000;
    private const int TituloMaxLength = 150;
    private const int LinkMaxLength = 500;
    private const int ArquivoUrlMaxLength = 500;
    private const int ArquivoNomeMaxLength = 255;
    private const string PastaArquivos = "/uploads/publicacoes/";

    private readonly List<ComentarioPublicacao> _comentarios = new();
    private readonly List<CurtidaPublicacao> _curtidas = new();

    public Guid Id { get; private set; }
    public Guid AutorId { get; private set; }
    public TipoPublicacao Tipo { get; private set; }
    public string? Titulo { get; private set; }
    public string Conteudo { get; private set; } = string.Empty;
    public string? LinkUrl { get; private set; }
    public string? ArquivoUrl { get; private set; }
    public string? ArquivoNome { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<ComentarioPublicacao> Comentarios => _comentarios.AsReadOnly();
    public IReadOnlyCollection<CurtidaPublicacao> Curtidas => _curtidas.AsReadOnly();

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected Publicacao()
    {
    }

    public Publicacao(
        Guid autorId,
        TipoPublicacao tipo,
        string conteudo,
        string? titulo = null,
        string? linkUrl = null,
        string? arquivoUrl = null,
        string? arquivoNome = null)
    {
        if (autorId == Guid.Empty)
            throw new DomainException("O autor da publicação é obrigatório.");

        if (!Enum.IsDefined(tipo))
            throw new DomainException("O tipo da publicação é inválido.");

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException("O conteúdo da publicação é obrigatório.");

        var conteudoNormalizado = conteudo.Trim();

        if (conteudoNormalizado.Length > ConteudoMaxLength)
            throw new DomainException($"O conteúdo da publicação deve ter no máximo {ConteudoMaxLength} caracteres.");

        Id = Guid.NewGuid();
        AutorId = autorId;
        Tipo = tipo;
        Conteudo = conteudoNormalizado;
        Titulo = NormalizarTitulo(titulo);
        LinkUrl = NormalizarLink(linkUrl);
        DefinirArquivo(arquivoUrl, arquivoNome);
        CriadoEm = DateTime.UtcNow;

        if (Tipo == TipoPublicacao.Link && LinkUrl is null)
            throw new DomainException("Publicações do tipo LINK precisam de um link.");

        if (Tipo == TipoPublicacao.Documento && ArquivoUrl is null)
            throw new DomainException("Publicações do tipo DOCUMENTO precisam de um anexo.");
    }

    /// <summary>
    /// Adiciona um comentário à publicação. Qualquer usuário autenticado pode comentar.
    /// </summary>
    public ComentarioPublicacao Comentar(Guid usuarioId, string conteudo)
    {
        var comentario = new ComentarioPublicacao(Id, usuarioId, conteudo);

        _comentarios.Add(comentario);

        return comentario;
    }

    /// <summary>
    /// Alterna a curtida do usuário: cria quando ainda não existe e remove quando já existe.
    /// </summary>
    /// <returns><c>true</c> se a publicação ficou curtida pelo usuário.</returns>
    public bool AlternarCurtida(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário da curtida é obrigatório.");

        var existente = _curtidas.SingleOrDefault(curtida => curtida.UsuarioId == usuarioId);

        if (existente is not null)
        {
            _curtidas.Remove(existente);
            return false;
        }

        _curtidas.Add(new CurtidaPublicacao(Id, usuarioId));
        return true;
    }

    /// <summary>
    /// Garante que o solicitante é o autor da publicação. Exposto para que a camada de
    /// aplicação valide a exclusão antes de remover o registro.
    /// </summary>
    public void GarantirAutor(Guid usuarioId)
    {
        if (usuarioId != AutorId)
            throw new DomainException("Apenas o autor pode excluir esta publicação.");
    }

    private void DefinirArquivo(string? arquivoUrl, string? arquivoNome)
    {
        var url = string.IsNullOrWhiteSpace(arquivoUrl) ? null : arquivoUrl.Trim();
        var nome = string.IsNullOrWhiteSpace(arquivoNome) ? null : arquivoNome.Trim();

        if ((url is null) != (nome is null))
            throw new DomainException("O anexo da publicação é inválido.");

        if (url is not null)
        {
            if (url.Length > ArquivoUrlMaxLength
                || !url.StartsWith(PastaArquivos, StringComparison.Ordinal))
                throw new DomainException("O caminho do anexo é inválido.");

            if (nome!.Length > ArquivoNomeMaxLength)
                throw new DomainException($"O nome do anexo deve ter no máximo {ArquivoNomeMaxLength} caracteres.");
        }

        ArquivoUrl = url;
        ArquivoNome = nome;
    }

    private static string? NormalizarTitulo(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return null;

        var normalizado = titulo.Trim();

        if (normalizado.Length > TituloMaxLength)
            throw new DomainException($"O título da publicação deve ter no máximo {TituloMaxLength} caracteres.");

        return normalizado;
    }

    private static string? NormalizarLink(string? linkUrl)
    {
        if (string.IsNullOrWhiteSpace(linkUrl))
            return null;

        var normalizado = linkUrl.Trim();

        if (normalizado.Length > LinkMaxLength)
            throw new DomainException($"O link deve ter no máximo {LinkMaxLength} caracteres.");

        if (!Uri.TryCreate(normalizado, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("O link deve começar com http:// ou https://.");

        return normalizado;
    }
}
