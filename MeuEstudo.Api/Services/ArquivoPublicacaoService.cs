using System.IO.Compression;
using System.Text;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Api.Services;

public sealed class ArquivoPublicacaoService : IArquivoPublicacaoService
{
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;
    private const int NomeMaxLength = 255;
    private const int BytesParaAnalise = 64 * 1024;
    private const string PastaRelativa = "uploads/publicacoes";

    /// <summary>
    /// Extensões aceitas, com o tipo real que o conteúdo precisa ter. A extensão
    /// sozinha nunca é suficiente: o conteúdo é inspecionado pelos magic bytes/estrutura.
    /// </summary>
    private static readonly Dictionary<string, TipoValidacao> TiposPermitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = TipoValidacao.Pdf,
            [".doc"] = TipoValidacao.Ole,
            [".docx"] = TipoValidacao.Docx,
            [".ppt"] = TipoValidacao.Ole,
            [".pptx"] = TipoValidacao.Pptx,
            [".txt"] = TipoValidacao.Texto,
            [".md"] = TipoValidacao.Texto,
            [".png"] = TipoValidacao.Png,
            [".jpg"] = TipoValidacao.Jpeg,
            [".jpeg"] = TipoValidacao.Jpeg,
            [".webp"] = TipoValidacao.Webp
        };

    /// <summary>Content-types claramente executáveis/ativos nunca são aceitos.</summary>
    private static readonly HashSet<string> ContentTypesBloqueados = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/x-msdownload",
        "application/x-dosexec",
        "application/x-msdos-program",
        "application/x-executable",
        "application/x-sh",
        "application/x-bat",
        "application/x-httpd-php",
        "text/html",
        "application/xhtml+xml",
        "image/svg+xml"
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

        if (!string.IsNullOrWhiteSpace(arquivo.ContentType)
            && ContentTypesBloqueados.Contains(arquivo.ContentType))
        {
            throw new DomainException("O tipo de conteúdo enviado não é permitido.");
        }

        var extensao = Path.GetExtension(arquivo.FileName)?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extensao) || !TiposPermitidos.TryGetValue(extensao, out var tipo))
            throw new DomainException(
                "Formato de anexo não suportado. Use PDF, DOC, DOCX, PPT, PPTX, TXT, MD, PNG, JPG ou WebP.");

        await using var conteudo = new MemoryStream((int)arquivo.Length);
        await arquivo.CopyToAsync(conteudo, cancellationToken);
        conteudo.Position = 0;

        if (!ConteudoCoerente(conteudo, tipo))
            throw new DomainException("O conteúdo do arquivo não corresponde ao formato informado.");

        var nomeOriginal = NormalizarNome(arquivo.FileName, extensao);
        var diretorio = Path.Combine(DiretorioBase(), usuarioId.ToString());
        Directory.CreateDirectory(diretorio);

        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

        try
        {
            conteudo.Position = 0;
            await using var destino = new FileStream(
                caminhoCompleto,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await conteudo.CopyToAsync(destino, cancellationToken);
        }
        catch
        {
            // Não deixa arquivo parcial para trás se a gravação falhar.
            if (File.Exists(caminhoCompleto))
                File.Delete(caminhoCompleto);

            throw;
        }

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

    private static bool ConteudoCoerente(Stream conteudo, TipoValidacao validacao)
    {
        conteudo.Position = 0;

        return validacao switch
        {
            TipoValidacao.Pdf => ComecaCom(conteudo, "%PDF-"u8),
            TipoValidacao.Png => ComecaCom(conteudo, PngAssinatura),
            TipoValidacao.Jpeg => ComecaCom(conteudo, [0xFF, 0xD8, 0xFF]),
            TipoValidacao.Webp => EhWebp(conteudo),
            TipoValidacao.Ole => ComecaCom(conteudo, OleAssinatura),
            TipoValidacao.Docx => EhOoxml(conteudo, "word/document.xml"),
            TipoValidacao.Pptx => EhOoxml(conteudo, "ppt/presentation.xml"),
            TipoValidacao.Texto => EhTexto(conteudo),
            _ => false
        };
    }

    private static bool ComecaCom(Stream conteudo, ReadOnlySpan<byte> assinatura)
    {
        Span<byte> cabecalho = stackalloc byte[assinatura.Length];

        conteudo.Position = 0;
        var lidos = conteudo.Read(cabecalho);
        conteudo.Position = 0;

        return lidos == assinatura.Length && cabecalho.SequenceEqual(assinatura);
    }

    private static bool EhWebp(Stream conteudo)
    {
        Span<byte> cabecalho = stackalloc byte[12];

        conteudo.Position = 0;
        var lidos = conteudo.Read(cabecalho);
        conteudo.Position = 0;

        return lidos == 12
               && cabecalho[..4].SequenceEqual("RIFF"u8)
               && cabecalho[8..12].SequenceEqual("WEBP"u8);
    }

    private static bool EhOoxml(Stream conteudo, string entradaObrigatoria)
    {
        if (!ComecaCom(conteudo, [0x50, 0x4B, 0x03, 0x04]))
            return false;

        try
        {
            conteudo.Position = 0;

            using var zip = new ZipArchive(conteudo, ZipArchiveMode.Read, leaveOpen: true);

            var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var total = 0;

            foreach (var entrada in zip.Entries)
            {
                // ZIPs com milhares de entradas são rejeitados antes de consumir CPU/memória.
                if (++total > 1000 || entrada.FullName.Length > 512)
                    return false;

                // Documentos com macros (vbaProject.bin) não podem ser aceitos como .docx/.pptx.
                if (entrada.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                    return false;

                nomes.Add(entrada.FullName);
            }

            return nomes.Contains("[Content_Types].xml") && nomes.Contains(entradaObrigatoria);
        }
        catch (InvalidDataException)
        {
            return false;
        }
        finally
        {
            conteudo.Position = 0;
        }
    }

    private static bool EhTexto(Stream conteudo)
    {
        var limite = (int)Math.Min(conteudo.Length, BytesParaAnalise);
        var bytes = new byte[limite];

        conteudo.Position = 0;
        var lidos = conteudo.Read(bytes, 0, limite);
        conteudo.Position = 0;

        if (lidos == 0)
            return false;

        var amostra = bytes.AsSpan(0, lidos);

        // NUL indica conteúdo binário disfarçado de texto.
        if (amostra.IndexOf((byte)0) >= 0)
            return false;

        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(amostra);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string NormalizarNome(string nomeInformado, string extensao)
    {
        var nome = Path.GetFileName(nomeInformado)?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            return $"material{extensao}";

        // Remove caracteres de controle/quebras de linha; o nome é apenas exibido.
        nome = new string(nome.Where(caractere => !char.IsControl(caractere)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(nome))
            return $"material{extensao}";

        if (nome.Length <= NomeMaxLength)
            return nome;

        var limite = NomeMaxLength - extensao.Length;

        return $"{nome[..limite]}{extensao}";
    }

    private static ReadOnlySpan<byte> PngAssinatura =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> OleAssinatura =>
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private enum TipoValidacao
    {
        Pdf,
        Docx,
        Pptx,
        Ole,
        Texto,
        Png,
        Jpeg,
        Webp
    }

}
