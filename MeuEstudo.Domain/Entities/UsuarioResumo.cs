namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Dados públicos mínimos de um usuário, usados para compor responsáveis por cards
/// sem carregar a entidade <see cref="Usuario"/> completa.
/// </summary>
public sealed record UsuarioResumo(Guid Id, string Nome, string? FotoUrl);
