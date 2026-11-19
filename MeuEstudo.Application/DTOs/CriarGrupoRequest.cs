namespace MeuEstudo.Application.DTOs;

/// <summary>
/// Dados para criação de um quadro/estudo. <c>Tipo</c> aceita PESSOAL ou TRABALHO_ACADEMICO;
/// quando omitido, assume TRABALHO_ACADEMICO.
/// </summary>
public sealed record CriarGrupoRequest(string Titulo, string? Descricao = null, string? Tipo = null);
