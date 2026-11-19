namespace MeuEstudos.Application.DTOs;

/// <summary>
/// Dados de criação de uma publicação. <c>ArquivoUrl</c> e <c>ArquivoNome</c> são preenchidos
/// pela API após o armazenamento do anexo, nunca diretamente pelo cliente.
/// </summary>
public sealed record CriarPublicacaoRequest(
    string? Tipo,
    string? Titulo,
    string? Conteudo,
    string? LinkUrl,
    string? ArquivoUrl = null,
    string? ArquivoNome = null);
