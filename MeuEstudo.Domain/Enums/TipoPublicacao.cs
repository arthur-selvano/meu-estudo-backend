namespace MeuEstudo.Domain.Enums;

/// <summary>
/// Natureza de uma publicação da comunidade: pedido de estudo, aula, link ou documento.
/// </summary>
public enum TipoPublicacao
{
    Estudo = 0,
    Aula = 1,
    Link = 2,
    Documento = 3
}

/// <summary>
/// Converte <see cref="TipoPublicacao"/> para os valores gravados em <c>publicacoes.tipo</c>.
/// </summary>
public static class TipoPublicacaoExtensions
{
    public static string ParaValorBanco(this TipoPublicacao tipo) => tipo switch
    {
        TipoPublicacao.Estudo => "ESTUDO",
        TipoPublicacao.Aula => "AULA",
        TipoPublicacao.Link => "LINK",
        TipoPublicacao.Documento => "DOCUMENTO",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de publicação inválido.")
    };

    public static TipoPublicacao DeValorBanco(string valor) => valor switch
    {
        "ESTUDO" => TipoPublicacao.Estudo,
        "AULA" => TipoPublicacao.Aula,
        "LINK" => TipoPublicacao.Link,
        "DOCUMENTO" => TipoPublicacao.Documento,
        _ => throw new ArgumentOutOfRangeException(nameof(valor), valor, "Tipo de publicação inválido no banco de dados.")
    };

    /// <summary>
    /// Interpreta o valor informado pela API sem diferenciar maiúsculas/minúsculas.
    /// </summary>
    public static bool TentarConverter(string? valor, out TipoPublicacao tipo)
    {
        switch (valor?.Trim().ToUpperInvariant())
        {
            case "ESTUDO":
                tipo = TipoPublicacao.Estudo;
                return true;
            case "AULA":
                tipo = TipoPublicacao.Aula;
                return true;
            case "LINK":
                tipo = TipoPublicacao.Link;
                return true;
            case "DOCUMENTO":
                tipo = TipoPublicacao.Documento;
                return true;
            default:
                tipo = TipoPublicacao.Estudo;
                return false;
        }
    }
}
