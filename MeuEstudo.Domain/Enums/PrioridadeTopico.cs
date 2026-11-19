namespace MeuEstudo.Domain.Enums;

/// <summary>
/// Prioridade de um card/tópico dentro do quadro Kanban.
/// </summary>
public enum PrioridadeTopico
{
    Baixa = 0,
    Media = 1,
    Alta = 2
}

/// <summary>
/// Converte <see cref="PrioridadeTopico"/> para os valores gravados em <c>topicos_estudo.prioridade</c>.
/// </summary>
public static class PrioridadeTopicoExtensions
{
    public static string ParaValorBanco(this PrioridadeTopico prioridade) => prioridade switch
    {
        PrioridadeTopico.Baixa => "BAIXA",
        PrioridadeTopico.Media => "MEDIA",
        PrioridadeTopico.Alta => "ALTA",
        _ => throw new ArgumentOutOfRangeException(nameof(prioridade), prioridade, "Prioridade inválida.")
    };

    public static PrioridadeTopico DeValorBanco(string valor) => valor switch
    {
        "BAIXA" => PrioridadeTopico.Baixa,
        "MEDIA" => PrioridadeTopico.Media,
        "ALTA" => PrioridadeTopico.Alta,
        _ => throw new ArgumentOutOfRangeException(nameof(valor), valor, "Prioridade inválida no banco de dados.")
    };

    /// <summary>
    /// Interpreta o valor informado pela API sem diferenciar maiúsculas/minúsculas.
    /// </summary>
    public static bool TentarConverter(string? valor, out PrioridadeTopico prioridade)
    {
        switch (valor?.Trim().ToUpperInvariant())
        {
            case "BAIXA":
                prioridade = PrioridadeTopico.Baixa;
                return true;
            case "MEDIA":
                prioridade = PrioridadeTopico.Media;
                return true;
            case "ALTA":
                prioridade = PrioridadeTopico.Alta;
                return true;
            default:
                prioridade = PrioridadeTopico.Media;
                return false;
        }
    }
}
