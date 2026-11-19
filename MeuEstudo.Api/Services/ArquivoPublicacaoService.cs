using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Api.Services;

public sealed class ArquivoPublicacaoService : IArquivoPublicacaoService
{
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;
    private const int NomeMaxLength = 255;
    private const string PastaRelativa = "uploads/publicacoes";

    private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".ppt",
        ".pptx",
        ".txt",
        ".md",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public ArquivoPublicacaoService(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task<ArquivoPublicacaoSalvo> SalvarAsync(
        Guid usuarioId,
        IFormFile arquivo,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário do anexo é obrigatório.");

        if (arquivo is null || arquivo.Length == 0)
            throw new DomainException("Selecione um arquivo para anexar.");

        if (arquivo.Length > TamanhoMaximoBytes)
            throw new DomainException("O anexo deve ter no máximo 10 MB.");

        var extensao = Path.GetExtension(arquivo.FileName)?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
            throw new DomainException(
                "Formato de anexo não suportado. Use PDF, DOC, DOCX, PPT, PPTX, TXT, MD, PNG, JPG ou WebP.");

        var nomeOriginal = NormalizarNome(arquivo.FileName, extensao);
        var diretorio = Path.Combine(DiretorioBase(), usuarioId.ToString());
        Directory.CreateDirectory(diretorio);

        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

        await using var destino = File.Create(caminhoCompleto);
        await arquivo.CopyToAsync(destino, cancellationToken);

        return new ArquivoPublicacaoSalvo(
            $"/{PastaRelativa}/{usuarioId}/{nomeArquivo}",
            nomeOriginal);
    }

    public Task RemoverAsync(string? arquivoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(arquivoUrl))
            return Task.CompletedTask;

        var prefixo = $"/{PastaRelativa}/";

        if (!arquivoUrl.StartsWith(prefixo, StringComparison.Ordinal))
            return Task.CompletedTask;

        var baseDiretorio = Path.GetFullPath(DiretorioBase()) + Path.DirectorySeparatorChar;
        var relativo = arquivoUrl[prefixo.Length..].Replace('/', Path.DirectorySeparatorChar);
        var caminho = Path.GetFullPath(Path.Combine(DiretorioBase(), relativo));

        // Só apaga dentro do diretório de publicações; um caminho manipulado não alcança outros arquivos.
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

        return Path.Combine(raiz, "uploads", "publicacoes");
    }

    private static string NormalizarNome(string nomeInformado, string extensao)
    {
        var nome = Path.GetFileName(nomeInformado)?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            return $"material{extensao}";

        if (nome.Length <= NomeMaxLength)
            return nome;

        var limite = NomeMaxLength - extensao.Length;

        return $"{nome[..limite]}{extensao}";
    }
}
