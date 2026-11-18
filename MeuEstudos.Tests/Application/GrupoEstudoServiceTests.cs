using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Services;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;
using MeuEstudos.Tests.Fakes;

namespace MeuEstudos.Tests.Application;

public sealed class GrupoEstudoServiceTests
{
    [Fact]
    public async Task AdicionarTopicoAsync_EncaminhaOGuidDaColunaDaRequisicao()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var colunaAFazer = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository());

        var resposta = await servico.AdicionarTopicoAsync(
            donoId,
            grupo.Id,
            new CriarTopicoRequest("COmer", colunaAFazer.Id));

        Assert.Equal(colunaAFazer.Id, resposta.ColunaId);
        Assert.Equal("A Fazer", grupo.Colunas.Single(coluna => coluna.Id == resposta.ColunaId).Titulo);
        Assert.Equal(resposta.Id, Assert.Single(grupo.Topicos).Id);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AlterarResponsavelTopicoAsync_AtribuiMembroERetornaDadosParaExibicao()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var membroId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        grupo.ConvidarMembro(donoId, membroId);
        grupo.ResponderConvite(membroId, true);
        var topico = grupo.AdicionarTopico(
            donoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card");

        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var usuarios = new FakeUsuarioRepository(
            new Dictionary<Guid, UsuarioResumo>
            {
                [membroId] = new(membroId, "Membro Aceito", "/uploads/perfis/membro.png")
            });
        var servico = new GrupoEstudoService(repositorio, usuarios);

        var resposta = await servico.AlterarResponsavelTopicoAsync(
            donoId,
            topico.Id,
            new AlterarResponsavelTopicoRequest(membroId));

        Assert.Equal(membroId, resposta.ResponsavelId);
        Assert.Equal("Membro Aceito", resposta.ResponsavelNome);
        Assert.Equal("/uploads/perfis/membro.png", resposta.ResponsavelFotoUrl);
        Assert.Equal(membroId, topico.ResponsavelId);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AlterarResponsavelTopicoAsync_RemoveResponsavelComNull()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var topico = grupo.AdicionarTopico(
            donoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card",
            responsavelId: donoId);
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository());

        var resposta = await servico.AlterarResponsavelTopicoAsync(
            donoId,
            topico.Id,
            new AlterarResponsavelTopicoRequest(null));

        Assert.Null(resposta.ResponsavelId);
        Assert.Null(resposta.ResponsavelNome);
        Assert.Null(topico.ResponsavelId);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AdicionarTopicoAsync_RejeitaColunaVaziaSemPersistir()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository());

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.AdicionarTopicoAsync(
                donoId,
                grupo.Id,
                new CriarTopicoRequest("COmer", Guid.Empty)));

        Assert.Equal("A coluna do card é obrigatória.", excecao.Message);
        Assert.Empty(grupo.Topicos);
        Assert.Equal(0, repositorio.Atualizacoes);
    }
}
