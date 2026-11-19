using MeuEstudo.Domain.Entities;

namespace MeuEstudo.Domain.Repositories;

/// <summary>
/// Contrato de persistência do agregado <see cref="Publicacao"/>.
/// </summary>
public interface IPublicacaoRepository
{
    /// <summary>
    /// Lista o feed completo, do mais recente para o mais antigo, com comentários e curtidas.
    /// </summary>
    Task<IReadOnlyCollection<Publicacao>> ObterTodasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Carrega o agregado completo (comentários e curtidas) para aplicar as regras de domínio.
    /// </summary>
    Task<Publicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Publicacao publicacao, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Publicacao publicacao, CancellationToken cancellationToken = default);

    Task RemoverAsync(Publicacao publicacao, CancellationToken cancellationToken = default);
}
