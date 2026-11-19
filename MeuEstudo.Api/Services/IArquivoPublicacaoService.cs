namespace MeuEstudo.Api.Services;

/// <summary>Anexo já armazenado, com o caminho público e o nome original do arquivo.</summary>
public sealed record ArquivoPublicacaoSalvo(string Url, string Nome);

/// <summary>
/// Armazenamento local dos anexos de publicações em <c>wwwroot/uploads/publicacoes</c>.
/// </summary>
public interface IArquivoPublicacaoService
{
    /// <summary>
    /// Valida e grava o arquivo, devolvendo o caminho público e o nome exibido.
    /// </summary>
    /// <exception cref="MeuEstudo.Domain.Exceptions.DomainException">Arquivo inválido ou grande demais.</exception>
    Task<ArquivoPublicacaoSalvo> SalvarAsync(
        Guid usuarioId,
        IFormFile arquivo,
        CancellationToken cancellationToken = default);

    /// <summary>Remove um anexo, ignorando caminhos fora do diretório de publicações.</summary>
    Task RemoverAsync(string? arquivoUrl, CancellationToken cancellationToken = default);
}
