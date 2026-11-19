namespace MeuEstudo.Application.DTOs;

/// <summary>Convite pendente de participação em um grupo de estudo.</summary>
public sealed record ConvitePendenteResponse(
    Guid GrupoId,
    string GrupoTitulo,
    Guid DonoId,
    string DonoNome,
    DateTime ConvidadoEm);
