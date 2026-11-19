namespace MeuEstudo.Application.DTOs;

/// <summary>Membro convidado de um grupo, com o nome atual do usuário e o status do convite.</summary>
public sealed record MembroGrupoResponse(Guid UsuarioId, string Nome, string Status);
