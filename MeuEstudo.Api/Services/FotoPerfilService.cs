using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Api.Services;

public sealed class FotoPerfilService : IFotoPerfilService
{
    private const long TamanhoMaximoBytes = 2 * 1024 * 1024;
    private const int BytesCabecalho = 12;
    private const string PastaRelativa = "uploads/perfis";

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IWebHostEnvironment _environment;

    public FotoPerfilService(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task<string> SalvarAsync(
        Guid usuarioId,
        IFormFile foto,
        CancellationToken cancellationToken = default)
    {
        if (foto is null || foto.Length == 0)
            throw new DomainException("Envie uma imagem para a foto de perfil.");

        if (foto.Length > TamanhoMaximoBytes)
            throw new DomainException("A foto deve ter no máximo 2 MB.");

        await using var conteudo = new MemoryStream((int)foto.Length);
        await foto.CopyToAsync(conteudo, cancellationToken);

        var tipo = DetectarTipo(conteudo);

        if (tipo is null || !TipoCoerente(foto, tipo.Value))
            throw new DomainException("Formato de imagem não suportado. Use JPEG, PNG ou WebP.");

        var diretorio = Path.Combine(DiretorioBase(), usuarioId.ToString());
        Directory.CreateDirectory(diretorio);

        var nomeArquivo = $"{Guid.NewGuid():N}{tipo.Value.Extensao}";
        var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

        conteudo.Position = 0;
        await using (var destino = new FileStream(
                         caminhoCompleto,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None))
        {
            await conteudo.CopyToAsync(destino, cancellationToken);
        }

        return $"/{PastaRelativa}/{usuarioId}/{nomeArquivo}";
    }

    public Task RemoverAsync(string? fotoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
            return Task.CompletedTask;

        var prefixo = $"/{PastaRelativa}/";

        if (!fotoUrl.StartsWith(prefixo, StringComparison.Ordinal))
            return Task.CompletedTask;

        var baseDiretorio = Path.GetFullPath(DiretorioBase()) + Path.DirectorySeparatorChar;
        var relativo = fotoUrl[prefixo.Length..].Replace('/', Path.DirectorySeparatorChar);
        var caminho = Path.GetFullPath(Path.Combine(DiretorioBase(), relativo));

        // Só apaga dentro do diretório de perfis; um caminho manipulado não alcança outros arquivos.
        if (!caminho.StartsWith(baseDiretorio, StringComparison.Ordinal))
            return Task.CompletedTask;

        if (File.Exists(caminho))
            File.Delete(caminho);

        return Task.CompletedTask;
    }

    private string DiretorioBase()
    {
        var raiz = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        return Path.Combine(raiz, "uploads", "perfis");
    }

    private static TipoImagem? DetectarTipo(Stream conteudo)
    {
        Span<byte> cabecalho = stackalloc byte[BytesCabecalho];

        conteudo.Position = 0;
        var lidos = conteudo.Read(cabecalho);
        conteudo.Position = 0;

        if (lidos < BytesCabecalho)
            return null;

        if (cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return new TipoImagem("image/jpeg", ".jpg");

        if (cabecalho[..8].SequenceEqual(PngAssinatura))
            return new TipoImagem("image/png", ".png");

        if (cabecalho[..4].SequenceEqual("RIFF"u8)
            && cabecalho.Slice(8, 4).SequenceEqual("WEBP"u8))
            return new TipoImagem("image/webp", ".webp");

        return null;
    }

    private static bool TipoCoerente(IFormFile foto, TipoImagem tipo)
    {
        if (!TiposPermitidos.Contains(foto.ContentType)
            || !string.Equals(foto.ContentType, tipo.ContentType, StringComparison.OrdinalIgnoreCase))
            return false;

        var extensao = Path.GetExtension(foto.FileName);

        return tipo.Extensao == ".jpg"
            ? extensao.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
              || extensao.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            : extensao.Equals(tipo.Extensao, StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlySpan<byte> PngAssinatura =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly record struct TipoImagem(string ContentType, string Extensao);
}
