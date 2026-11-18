namespace MeuEstudos.Api.Services;

/// <summary>
/// Armazenamento local das fotos de perfil em <c>wwwroot/uploads/perfis</c>.
/// </summary>
public interface IFotoPerfilService
{
    /// <summary>
    /// Valida a imagem e grava o arquivo, devolvendo o caminho público relativo.
    /// </summary>
    /// <exception cref="MeuEstudos.Domain.Exceptions.DomainException">Imagem inválida ou grande demais.</exception>
    Task<string> SalvarAsync(
        Guid usuarioId,
        IFormFile foto,
        CancellationToken cancellationToken = default);

    /// <summary>Remove uma foto anterior, ignorando caminhos fora do diretório de perfis.</summary>
    Task RemoverAsync(string? fotoUrl, CancellationToken cancellationToken = default);
}
