using System.IO.Compression;
using MeuEstudo.Api.Services;
using MeuEstudo.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace MeuEstudo.Tests.Api;

/// <summary>
/// Uploads são validados pelo conteúdo real (magic bytes/estrutura), não só pela
/// extensão, e a remoção nunca escapa do diretório de uploads.
/// </summary>
public sealed class UploadSecurityTests : IDisposable
{
    private static readonly byte[] PngValido = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private static readonly byte[] JpegValido = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    private readonly string _raiz;
    private readonly AmbienteFake _ambiente;
    private readonly ArquivoPublicacaoService _anexos;
    private readonly FotoPerfilService _fotos;

    public UploadSecurityTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), $"meuestudo-testes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_raiz);
        _ambiente = new AmbienteFake(_raiz);
        _anexos = new ArquivoPublicacaoService(_ambiente);
        _fotos = new FotoPerfilService(_ambiente);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
            Directory.Delete(_raiz, recursive: true);
    }

    [Theory]
    [InlineData("malware.exe", "application/octet-stream")]
    [InlineData("script.sh", "text/x-shellscript")]
    [InlineData("pagina.html", "text/html")]
    [InlineData("desenho.svg", "image/svg+xml")]
    public async Task Anexo_RejeitaExtensoesNaoPermitidas(string nome, string contentType)
    {
        var arquivo = Arquivo([0x4D, 0x5A, 0x90, 0x00], nome, contentType);

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_RejeitaPdfComConteudoQueNaoEhPdf()
    {
        var arquivo = Arquivo([0x4D, 0x5A, 0x90, 0x00], "documento.pdf", "application/pdf");

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_RejeitaDocxDisfarcadoDeTexto()
    {
        var arquivo = Arquivo(
            "conteúdo de texto, não um zip"u8.ToArray(),
            "documento.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_RejeitaPngComConteudoBinarioQualquer()
    {
        var arquivo = Arquivo([0x4D, 0x5A, 0x90, 0x00, 0x03], "imagem.png", "image/png");

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_RejeitaArquivoAcimaDoLimite()
    {
        var arquivo = Arquivo(new byte[10 * 1024 * 1024 + 1], "grande.pdf", "application/pdf");

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_AceitaPngECriaNomeAleatorioForaDoNomeOriginal()
    {
        var usuarioId = Guid.NewGuid();
        var arquivo = Arquivo(PngValido, "../../foto original.png", "image/png");

        var salvo = await _anexos.SalvarAsync(usuarioId, arquivo);

        Assert.StartsWith($"/uploads/publicacoes/{usuarioId}/", salvo.Url);
        Assert.EndsWith(".png", salvo.Url);
        Assert.Equal("foto original.png", salvo.Nome);

        var nomeArquivo = Path.GetFileName(salvo.Url);
        var semExtensao = Path.GetFileNameWithoutExtension(nomeArquivo);

        Assert.True(Guid.TryParseExact(semExtensao, "N", out _));
        Assert.True(File.Exists(CaminhoDoUrl(salvo.Url)));
    }

    [Fact]
    public async Task Anexo_AceitaDocxValido()
    {
        var arquivo = Arquivo(
            CriarDocx(),
            "trabalho.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var salvo = await _anexos.SalvarAsync(Guid.NewGuid(), arquivo);

        Assert.EndsWith(".docx", salvo.Url);
        Assert.True(File.Exists(CaminhoDoUrl(salvo.Url)));
    }

    [Fact]
    public async Task Anexo_RejeitaOoxmlComMacros()
    {
        var arquivo = Arquivo(
            CriarDocx(comMacro: true),
            "documento.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await Assert.ThrowsAsync<DomainException>(
            () => _anexos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Anexo_RemocaoNaoEscapaDoDiretorioDeUploads()
    {
        var sentinela = Path.Combine(_ambiente.WebRootPath, "segredo.txt");
        await File.WriteAllTextAsync(sentinela, "não apagar");

        await _anexos.RemoverAsync("/uploads/publicacoes/../../segredo.txt");

        Assert.True(File.Exists(sentinela));
    }

    [Fact]
    public async Task Anexo_RemocaoApagaSomenteOArquivoSalvo()
    {
        var salvo = await _anexos.SalvarAsync(Guid.NewGuid(), Arquivo(PngValido, "imagem.png", "image/png"));
        var caminho = CaminhoDoUrl(salvo.Url);

        await _anexos.RemoverAsync(salvo.Url);

        Assert.False(File.Exists(caminho));
    }

    [Fact]
    public async Task Foto_RejeitaHtmlDisfarcadoDePng()
    {
        var arquivo = Arquivo(
            "<html><script>alert(1)</script></html>"u8.ToArray(),
            "foto.png",
            "image/png");

        await Assert.ThrowsAsync<DomainException>(
            () => _fotos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Foto_RejeitaConteudoIncoerenteComOContentType()
    {
        var arquivo = Arquivo(JpegValido, "foto.png", "image/png");

        await Assert.ThrowsAsync<DomainException>(
            () => _fotos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Foto_RejeitaArquivoAcimaDe2MB()
    {
        var bytes = new byte[2 * 1024 * 1024 + 1];
        PngValido.CopyTo(bytes, 0);
        var arquivo = Arquivo(bytes, "foto.png", "image/png");

        await Assert.ThrowsAsync<DomainException>(
            () => _fotos.SalvarAsync(Guid.NewGuid(), arquivo));
    }

    [Fact]
    public async Task Foto_AceitaPngEGravaNoDiretorioDoProprioUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var arquivo = Arquivo(PngValido, "minha foto.png", "image/png");

        var url = await _fotos.SalvarAsync(usuarioId, arquivo);

        Assert.StartsWith($"/uploads/perfis/{usuarioId}/", url);
        Assert.True(File.Exists(CaminhoDoUrl(url)));
        Assert.True(Guid.TryParseExact(
            Path.GetFileNameWithoutExtension(Path.GetFileName(url)),
            "N",
            out _));
    }

    [Fact]
    public async Task Foto_RemocaoNaoEscapaDoDiretorioDePerfis()
    {
        var sentinela = Path.Combine(_ambiente.WebRootPath, "segredo.txt");
        await File.WriteAllTextAsync(sentinela, "não apagar");

        await _fotos.RemoverAsync("/uploads/perfis/../../segredo.txt");

        Assert.True(File.Exists(sentinela));
    }

    private string CaminhoDoUrl(string url)
        => Path.Combine(
            _ambiente.WebRootPath,
            url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    private static IFormFile Arquivo(byte[] bytes, string nome, string contentType)
        => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "arquivo", nome)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private static byte[] CriarDocx(bool comMacro = false)
    {
        using var memoria = new MemoryStream();

        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var tipos = new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open()))
                tipos.Write("<Types/>");

            using (var documento = new StreamWriter(zip.CreateEntry("word/document.xml").Open()))
                documento.Write("<w:document/>");

            if (comMacro)
            {
                using var macro = zip.CreateEntry("word/vbaProject.bin").Open();
                macro.Write([0x00, 0x01, 0x02]);
            }
        }

        return memoria.ToArray();
    }

    private sealed class AmbienteFake : IWebHostEnvironment
    {
        public AmbienteFake(string raiz)
        {
            ContentRootPath = raiz;
            WebRootPath = Path.Combine(raiz, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Testes";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
    }
}
