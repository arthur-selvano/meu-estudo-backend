namespace MeuEstudo.Application.DTOs;

/// <summary>
/// Define o responsável por um card. <c>ResponsavelId</c> nulo remove a atribuição;
/// quando informado, deve ser o dono ou um membro com convite aceito do grupo.
/// </summary>
public sealed record AlterarResponsavelTopicoRequest(Guid? ResponsavelId = null);
