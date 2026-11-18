namespace MeuEstudos.Application.DTOs;

/// <summary>
/// Dados para criação de um card/tópico. <c>Prioridade</c> aceita BAIXA, MEDIA ou ALTA
/// (padrão MEDIA). O responsável precisa ser o dono ou um membro com convite aceito.
/// </summary>
public sealed record CriarTopicoRequest(
    string Titulo,
    Guid ColunaId,
    string? Descricao = null,
    Guid? ResponsavelId = null,
    string? Prioridade = null,
    DateOnly? DataPrevista = null);
