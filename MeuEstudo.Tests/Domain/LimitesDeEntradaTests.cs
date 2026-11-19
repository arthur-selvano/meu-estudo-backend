using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Tests.Domain;

/// <summary>
/// Limites de tamanho no domínio evitam abuso de armazenamento e payloads gigantes.
/// </summary>
public sealed class LimitesDeEntradaTests
{
    private static readonly Guid DonoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");

    [Fact]
    public void Grupo_RejeitaDescricaoAcimaDe2000Caracteres()
    {
        var excecao = Assert.Throws<DomainException>(
            () => new GrupoEstudo("Grupo", new string('a', 2001), TipoGrupo.TrabalhoAcademico, DonoId));

        Assert.Contains("2000", excecao.Message);
    }

    [Fact]
    public void Grupo_AceitaDescricaoNoLimite()
    {
        var grupo = new GrupoEstudo("Grupo", new string('a', 2000), TipoGrupo.TrabalhoAcademico, DonoId);

        Assert.Equal(2000, grupo.Descricao!.Length);
    }

    [Fact]
    public void Topico_RejeitaDescricaoAcimaDe5000Caracteres()
    {
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, DonoId);
        var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AdicionarTopico(DonoId, coluna.Id, "Card", new string('a', 5001)));

        Assert.Contains("5000", excecao.Message);
        Assert.Empty(grupo.Topicos);
    }
}
