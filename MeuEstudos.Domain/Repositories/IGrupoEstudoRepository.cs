using MeuEstudos.Domain.Entities;

namespace MeuEstudos.Domain.Repositories;

/// <summary>
/// Contrato de persistência do agregado <see cref="GrupoEstudo"/>.
/// </summary>
public interface IGrupoEstudoRepository
{
    /// <summary>
    /// Carrega o agregado completo, incluindo <see cref="GrupoEstudo.Membros"/>,
    /// <see cref="GrupoEstudo.Colunas"/> e <see cref="GrupoEstudo.Topicos"/>. Isso é obrigatório
    /// para que as regras do agregado (convite duplicado, permissões, colunas e movimentação de cards)
    /// possam ser avaliadas.
    /// </summary>
    Task<GrupoEstudo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carrega o agregado completo que contém o tópico informado, permitindo validar
    /// permissões ao mover um card sem depender do id do grupo na rota.
    /// </summary>
    Task<GrupoEstudo?> ObterPorTopicoIdAsync(Guid topicoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os grupos criados por um usuário.
    /// </summary>
    Task<IReadOnlyCollection<GrupoEstudo>> ObterPorDonoAsync(Guid donoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os grupos dos quais o usuário participa como dono ou como membro com convite aceito.
    /// </summary>
    Task<IReadOnlyCollection<GrupoEstudo>> ObterDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os grupos em que o usuário possui convite com status <c>Pendente</c>,
    /// carregando os membros necessários para compor a resposta do convite.
    /// </summary>
    Task<IReadOnlyCollection<GrupoEstudo>> ObterComConvitePendenteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);

    Task AdicionarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default);

    Task AtualizarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o grupo e, em cascata, seus membros, colunas e tópicos.
    /// </summary>
    Task RemoverAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default);
}
