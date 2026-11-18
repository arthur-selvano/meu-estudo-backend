using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Tests.Domain;

public sealed class GrupoEstudoKanbanTests
{
    private static readonly Guid DonoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");

    private static GrupoEstudo CriarGrupo()
        => new("Grupo de estudos", null, TipoGrupo.TrabalhoAcademico, DonoId);

    private static List<ColunaKanban> ColunasOrdenadas(GrupoEstudo grupo)
        => grupo.Colunas.OrderBy(coluna => coluna.Ordem).ToList();

    [Fact]
    public void Construtor_CriaAsQuatroColunasPadraoComGuidsProprios()
    {
        var grupo = CriarGrupo();
        var colunas = ColunasOrdenadas(grupo);

        Assert.Equal(
            new[] { "A Fazer", "Em Andamento", "Revisão", "Concluído" },
            colunas.Select(coluna => coluna.Titulo).ToArray());
        Assert.Equal(new[] { 0, 1, 2, 3 }, colunas.Select(coluna => coluna.Ordem).ToArray());
        Assert.All(colunas, coluna =>
        {
            Assert.NotEqual(Guid.Empty, coluna.Id);
            Assert.Equal(grupo.Id, coluna.GrupoId);
        });
        Assert.Equal(4, colunas.Select(coluna => coluna.Id).Distinct().Count());
    }

    [Fact]
    public void AdicionarTopico_UsaExatamenteOGuidDaColunaInformada()
    {
        var grupo = CriarGrupo();
        var aFazer = ColunasOrdenadas(grupo)[0];

        var topico = grupo.AdicionarTopico(DonoId, aFazer.Id, "Revisar LINQ");

        Assert.Equal(aFazer.Id, topico.ColunaId);
        Assert.Equal(0, topico.Ordem);
        Assert.Same(topico, Assert.Single(grupo.Topicos));
    }

    [Fact]
    public void AdicionarTopico_NaoRecriaNemAlteraOsGuidsDasColunas()
    {
        var grupo = CriarGrupo();
        var guidsAntes = ColunasOrdenadas(grupo).Select(coluna => coluna.Id).ToArray();
        var aFazer = ColunasOrdenadas(grupo)[0];

        grupo.AdicionarTopico(DonoId, aFazer.Id, "Revisar LINQ");

        Assert.Equal(guidsAntes, ColunasOrdenadas(grupo).Select(coluna => coluna.Id).ToArray());
    }

    [Fact]
    public void AdicionarTopico_RejeitaColunaDeOutroGrupo()
    {
        var grupo = CriarGrupo();
        var outroGrupo = CriarGrupo();
        var colunaDeOutroGrupo = ColunasOrdenadas(outroGrupo)[0];

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AdicionarTopico(DonoId, colunaDeOutroGrupo.Id, "Card indevido"));

        Assert.Equal("Coluna não encontrada neste grupo.", excecao.Message);
        Assert.Empty(grupo.Topicos);
    }

    [Fact]
    public void AdicionarTopico_RejeitaColunaVaziaComMensagemEspecifica()
    {
        var grupo = CriarGrupo();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AdicionarTopico(DonoId, Guid.Empty, "Card sem coluna"));

        Assert.Equal("A coluna do card é obrigatória.", excecao.Message);
        Assert.Empty(grupo.Topicos);
    }

    [Fact]
    public void AdicionarTopico_NaUltimaColunaMarcaComoConcluido()
    {
        var grupo = CriarGrupo();
        var concluido = ColunasOrdenadas(grupo)[^1];

        var topico = grupo.AdicionarTopico(DonoId, concluido.Id, "Entrega final");

        Assert.Equal(concluido.Id, topico.ColunaId);
        Assert.True(topico.Concluido);
    }

    [Fact]
    public void AdicionarTopico_EmColunaIntermediariaNaoMarcaComoConcluido()
    {
        var grupo = CriarGrupo();
        var revisao = ColunasOrdenadas(grupo)[2];

        var topico = grupo.AdicionarTopico(DonoId, revisao.Id, "Revisar texto");

        Assert.Equal(revisao.Id, topico.ColunaId);
        Assert.False(topico.Concluido);
    }

    [Fact]
    public void MoverTopico_EntreColunasPreservaGuidsEMarcaConclusao()
    {
        var grupo = CriarGrupo();
        var colunas = ColunasOrdenadas(grupo);
        var aFazer = colunas[0];
        var concluido = colunas[^1];
        var topico = grupo.AdicionarTopico(DonoId, aFazer.Id, "Revisar LINQ");

        grupo.MoverTopico(DonoId, topico.Id, concluido.Id, 0);

        Assert.Equal(concluido.Id, topico.ColunaId);
        Assert.True(topico.Concluido);

        grupo.MoverTopico(DonoId, topico.Id, aFazer.Id, 0);

        Assert.Equal(aFazer.Id, topico.ColunaId);
        Assert.False(topico.Concluido);
        Assert.Equal(colunas.Select(coluna => coluna.Id), ColunasOrdenadas(grupo).Select(coluna => coluna.Id));
    }
}
