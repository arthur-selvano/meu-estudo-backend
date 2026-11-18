namespace MeuEstudos.Domain.Enums;

/// <summary>
/// Natureza do quadro: pessoal (privado ao dono) ou de trabalho acadêmico (compartilhável).
/// </summary>
public enum TipoGrupo
{
    Pessoal = 0,
    TrabalhoAcademico = 1
}

/// <summary>
/// Converte <see cref="TipoGrupo"/> para os valores gravados em <c>grupos_estudo.tipo</c>.
/// </summary>
public static class TipoGrupoExtensions
{
    public static string ParaValorBanco(this TipoGrupo tipo) => tipo switch
    {
        TipoGrupo.Pessoal => "PESSOAL",
        TipoGrupo.TrabalhoAcademico => "TRABALHO_ACADEMICO",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de grupo inválido.")
    };

    public static TipoGrupo DeValorBanco(string valor) => valor switch
    {
        "PESSOAL" => TipoGrupo.Pessoal,
        "TRABALHO_ACADEMICO" => TipoGrupo.TrabalhoAcademico,
        _ => throw new ArgumentOutOfRangeException(nameof(valor), valor, "Tipo de grupo inválido no banco de dados.")
    };

    /// <summary>
    /// Interpreta o valor informado pela API sem diferenciar maiúsculas/minúsculas.
    /// </summary>
    public static bool TentarConverter(string? valor, out TipoGrupo tipo)
    {
        switch (valor?.Trim().ToUpperInvariant())
        {
            case "PESSOAL":
                tipo = TipoGrupo.Pessoal;
                return true;
            case "TRABALHO_ACADEMICO":
                tipo = TipoGrupo.TrabalhoAcademico;
                return true;
            default:
                tipo = TipoGrupo.TrabalhoAcademico;
                return false;
        }
    }
}
